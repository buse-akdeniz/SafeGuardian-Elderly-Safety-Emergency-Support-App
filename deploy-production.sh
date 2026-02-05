#!/bin/bash

##############################################################################
# VitaGuard Production Deployment Script
# Purpose: Automated build, package, and deployment to Azure
# Usage: ./deploy-production.sh [environment] [version]
##############################################################################

set -e  # Exit on error

# Color codes for output
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Configuration
ENVIRONMENT=${1:-"staging"}
VERSION=${2:-"1.0.0"}
PROJECT_NAME="AsistanApp"
RESOURCE_GROUP="vitaguard-rg"
APP_SERVICE_NAME="vitaguard-${ENVIRONMENT}"
REGISTRY_NAME="vitaguardregistry"
ARTIFACT_DIR="./artifacts"
BUILD_OUTPUT_DIR="./publish"

# Helper functions
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[✓]${NC} $1"
}

log_error() {
    echo -e "${RED}[✗]${NC} $1"
    exit 1
}

log_warning() {
    echo -e "${YELLOW}[!]${NC} $1"
}

##############################################################################
# PHASE 1: Pre-deployment Checks
##############################################################################

phase_precheck() {
    log_info "=== PHASE 1: Pre-deployment Checks ==="
    
    # Check required tools
    log_info "Checking required tools..."
    command -v dotnet >/dev/null 2>&1 || log_error "dotnet CLI not found"
    command -v az >/dev/null 2>&1 || log_error "Azure CLI not found"
    command -v git >/dev/null 2>&1 || log_error "Git not found"
    
    # Check git status
    log_info "Checking Git status..."
    if [ -n "$(git status --porcelain)" ]; then
        log_warning "Uncommitted changes detected. Please commit before deploying."
        log_info "Continue anyway? (y/n)"
        read -r response
        [[ "$response" == "y" ]] || log_error "Deployment cancelled"
    fi
    
    # Verify Azure login
    log_info "Verifying Azure CLI authentication..."
    az account show >/dev/null 2>&1 || log_error "Not logged into Azure. Run: az login"
    
    log_success "Pre-deployment checks passed"
    echo ""
}

##############################################################################
# PHASE 2: Build & Test
##############################################################################

phase_build() {
    log_info "=== PHASE 2: Build & Test ==="
    
    # Clean previous builds
    log_info "Cleaning previous builds..."
    rm -rf "$BUILD_OUTPUT_DIR" "$ARTIFACT_DIR" bin/ obj/
    
    # Restore dependencies
    log_info "Restoring NuGet packages..."
    dotnet restore $PROJECT_NAME/$PROJECT_NAME.csproj || log_error "NuGet restore failed"
    
    # Run unit tests (if exist)
    if [ -f "$PROJECT_NAME.Tests/$PROJECT_NAME.Tests.csproj" ]; then
        log_info "Running unit tests..."
        dotnet test $PROJECT_NAME.Tests/$PROJECT_NAME.Tests.csproj \
            --configuration Release \
            --no-build \
            --verbosity quiet || log_error "Tests failed"
        log_success "All tests passed"
    else
        log_warning "No test project found, skipping tests"
    fi
    
    # Build release
    log_info "Building release configuration..."
    dotnet build $PROJECT_NAME/$PROJECT_NAME.csproj \
        --configuration Release \
        --no-restore \
        --verbosity minimal || log_error "Build failed"
    
    # Publish to folder
    log_info "Publishing application..."
    dotnet publish $PROJECT_NAME/$PROJECT_NAME.csproj \
        --configuration Release \
        --output "$BUILD_OUTPUT_DIR" \
        --no-build \
        --verbosity minimal || log_error "Publish failed"
    
    log_success "Build and publish completed successfully"
    echo ""
}

##############################################################################
# PHASE 3: Create Deployment Package
##############################################################################

phase_package() {
    log_info "=== PHASE 3: Create Deployment Package ==="
    
    mkdir -p "$ARTIFACT_DIR"
    
    # Create zip for Azure deployment
    log_info "Creating deployment package..."
    cd "$BUILD_OUTPUT_DIR"
    zip -r -q "../${ARTIFACT_DIR}/vitaguard-${VERSION}.zip" .
    cd ..
    
    log_success "Deployment package created: ${ARTIFACT_DIR}/vitaguard-${VERSION}.zip"
    log_info "Package size: $(du -h ${ARTIFACT_DIR}/vitaguard-${VERSION}.zip | cut -f1)"
    echo ""
}

##############################################################################
# PHASE 4: Deploy to Azure
##############################################################################

phase_azure_deploy() {
    log_info "=== PHASE 4: Deploy to Azure ==="
    
    # Check if resource group exists
    log_info "Checking Azure Resource Group..."
    if ! az group show --name "$RESOURCE_GROUP" &>/dev/null; then
        log_warning "Resource group '$RESOURCE_GROUP' does not exist"
        log_info "Creating resource group..."
        az group create \
            --name "$RESOURCE_GROUP" \
            --location "eastus" || log_error "Failed to create resource group"
        log_success "Resource group created"
    else
        log_success "Resource group found: $RESOURCE_GROUP"
    fi
    
    # Check if App Service exists
    log_info "Checking Azure App Service..."
    if ! az webapp show --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" &>/dev/null; then
        log_warning "App Service '$APP_SERVICE_NAME' does not exist"
        log_info "Creating App Service Plan..."
        az appservice plan create \
            --name "${APP_SERVICE_NAME}-plan" \
            --resource-group "$RESOURCE_GROUP" \
            --sku B2 \
            --is-linux || log_error "Failed to create App Service Plan"
        
        log_info "Creating App Service..."
        az webapp create \
            --name "$APP_SERVICE_NAME" \
            --resource-group "$RESOURCE_GROUP" \
            --plan "${APP_SERVICE_NAME}-plan" \
            --runtime "DOTNETCORE|8.0" || log_error "Failed to create App Service"
        
        log_success "App Service created"
    else
        log_success "App Service found: $APP_SERVICE_NAME"
    fi
    
    # Deploy application
    log_info "Deploying application to Azure..."
    az webapp deployment source config-zip \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --src "${ARTIFACT_DIR}/vitaguard-${VERSION}.zip" || log_error "Deployment failed"
    
    # Get deployment URL
    DEPLOYMENT_URL=$(az webapp show --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" --query defaultHostName -o tsv)
    log_success "Application deployed successfully"
    log_info "URL: https://${DEPLOYMENT_URL}"
    echo ""
}

##############################################################################
# PHASE 5: Post-deployment Verification
##############################################################################

phase_verify() {
    log_info "=== PHASE 5: Post-deployment Verification ==="
    
    DEPLOYMENT_URL=$(az webapp show --name "$APP_SERVICE_NAME" --resource-group "$RESOURCE_GROUP" --query defaultHostName -o tsv)
    
    # Wait for app to start
    log_info "Waiting for application to start..."
    for i in {1..30}; do
        if curl -s -f "https://${DEPLOYMENT_URL}/health" >/dev/null 2>&1; then
            log_success "Application is healthy"
            break
        fi
        log_info "Waiting... ($i/30)"
        sleep 2
    done
    
    # Check Swagger endpoint
    log_info "Checking Swagger documentation endpoint..."
    if curl -s -f "https://${DEPLOYMENT_URL}/swagger" >/dev/null 2>&1; then
        log_success "Swagger endpoint is available"
    else
        log_warning "Swagger endpoint not responding (may be disabled in production)"
    fi
    
    log_success "Deployment verification completed"
    echo ""
}

##############################################################################
# PHASE 6: SSL Certificate Configuration
##############################################################################

phase_ssl() {
    log_info "=== PHASE 6: SSL Certificate Configuration ==="
    
    log_info "Enabling HTTPS only..."
    az webapp update \
        --name "$APP_SERVICE_NAME" \
        --resource-group "$RESOURCE_GROUP" \
        --set httpsOnly=true || log_warning "Failed to enable HTTPS only"
    
    log_success "SSL configuration completed"
    echo ""
}

##############################################################################
# PHASE 7: Monitoring & Alerting Setup
##############################################################################

phase_monitoring() {
    log_info "=== PHASE 7: Monitoring & Alerting Setup ==="
    
    log_info "Creating Application Insights resource..."
    
    APP_INSIGHTS_NAME="vitaguard-${ENVIRONMENT}-insights"
    
    # Check if Application Insights exists
    if ! az monitor app-insights component show \
        --app "$APP_INSIGHTS_NAME" \
        --resource-group "$RESOURCE_GROUP" &>/dev/null; then
        
        log_info "Creating new Application Insights instance..."
        az monitor app-insights component create \
            --app "$APP_INSIGHTS_NAME" \
            --location "eastus" \
            --kind web \
            --resource-group "$RESOURCE_GROUP" \
            --application-type web \
            --retention-time 30 || log_warning "Failed to create Application Insights"
    else
        log_success "Application Insights already exists"
    fi
    
    log_success "Monitoring setup completed"
    echo ""
}

##############################################################################
# Main Execution
##############################################################################

main() {
    echo ""
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${BLUE}  VitaGuard Production Deployment${NC}"
    echo -e "${BLUE}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${YELLOW}Environment:${NC} $ENVIRONMENT"
    echo -e "${YELLOW}Version:${NC} $VERSION"
    echo -e "${YELLOW}Date:${NC} $(date '+%Y-%m-%d %H:%M:%S')"
    echo ""
    
    # Execute deployment phases
    phase_precheck
    phase_build
    phase_package
    phase_azure_deploy
    phase_verify
    phase_ssl
    phase_monitoring
    
    echo ""
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
    echo -e "${GREEN}  ✓ DEPLOYMENT COMPLETED SUCCESSFULLY${NC}"
    echo -e "${GREEN}═══════════════════════════════════════════════════════════════${NC}"
    echo ""
    echo -e "${YELLOW}Next Steps:${NC}"
    echo "1. Verify application at: https://${DEPLOYMENT_URL}"
    echo "2. Configure custom domain: https://vitaguard.app"
    echo "3. Setup Database connections in Azure Portal"
    echo "4. Configure email service (SendGrid/Office365)"
    echo "5. Enable SSL certificate (Let's Encrypt)"
    echo ""
}

# Run main function
main
