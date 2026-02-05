# VitaGuard Security Configuration
# Best practices for production deployment

## HTTPS/SSL Configuration
# Enable HTTPS only in production
Kestrel:Endpoints:Http:Url: disabled
Kestrel:Endpoints:Https:Url: https://0.0.0.0:443
Kestrel:Endpoints:Https:Certificate:Path: /etc/ssl/certs/vitaguard.pfx
Kestrel:Endpoints:Https:Certificate:Password: ${SSL_CERT_PASSWORD}

## CORS Policy (Cross-Origin Resource Sharing)
# Allow only trusted domains
CORS:AllowedOrigins:
  - https://vitaguard.app
  - https://www.vitaguard.app
  - https://family.vitaguard.app
  - https://api.vitaguard.app
  # Explicitly exclude:
  # - http:// (non-HTTPS)
  # - * (wildcard)
  # - localhost (development only)

CORS:AllowedMethods: GET, POST, PUT, DELETE, OPTIONS
CORS:AllowedHeaders: Content-Type, Authorization, X-Requested-With
CORS:AllowCredentials: true
CORS:MaxAge: 3600

## Security Headers
# Add security headers to all responses
SecurityHeaders:
  StrictTransportSecurity: max-age=31536000; includeSubDomains; preload
  XContentTypeOptions: nosniff
  XFrameOptions: DENY
  XSSProtection: 1; mode=block
  ContentSecurityPolicy: default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline'
  ReferrerPolicy: strict-origin-when-cross-origin
  PermissionsPolicy: geolocation=(), microphone=(), camera=()

## Rate Limiting
# Prevent brute force and DDoS attacks
RateLimiting:
  Enabled: true
  RequestsPerMinute: 100
  BurstLimit: 150
  
  # Per-endpoint limits
  Endpoints:
    /api/auth/login:
      RequestsPerMinute: 5
      LockoutMinutes: 15
    /api/emergency-alert:
      RequestsPerMinute: 10
    /api/health:
      RequestsPerMinute: 30

## Authentication & Authorization
JWT:
  SecretKey: ${JWT_SECRET_KEY}  # Change in production
  ExpirationMinutes: 1440  # 24 hours
  Issuer: vitaguard.app
  Audience: vitaguard-users
  
  # Token refresh
  RefreshTokenExpiration: 7  # days
  AllowMultipleTokens: false

## Data Encryption
DataEncryption:
  Enabled: true
  Algorithm: AES-256-GCM
  KeyRotationSchedule: monthly
  KeyStoragePath: /var/lib/vitaguard/keys
  
  # Sensitive fields to encrypt
  EncryptedFields:
    - HealthRecords
    - FamilyContactInfo
    - LocationData
    - PhoneNumbers

## Password Policy
PasswordPolicy:
  MinimumLength: 12
  RequireUppercase: true
  RequireLowercase: true
  RequireNumbers: true
  RequireSpecialCharacters: true
  ExpirationDays: 90
  HistoryCount: 5  # Prevent reusing last 5 passwords

## Database Security
DatabaseSecurity:
  ConnectionEncryption: TLS 1.3
  ConnectionTimeout: 30
  CommandTimeout: 300
  
  # SQL Injection Prevention
  ParameterizedQueries: true
  PreparedStatements: true
  InputValidation: strict
  
  # SQL Server specific
  TrustServerCertificate: false
  Encrypt: true

## Logging & Monitoring
Logging:
  # Do NOT log sensitive data
  ExcludePatterns:
    - password
    - token
    - creditcard
    - ssn
    - phone
    - email
    - address
  
  # Log security events
  SecurityEvents:
    - AuthenticationFailure
    - AuthorizationFailure
    - DataAccessError
    - ConfigurationChange
    - CertificateExpiration
  
  # Retention policy
  LogRetentionDays: 90
  ArchiveLogsAfterDays: 30

## Application Insights (Azure Monitoring)
ApplicationInsights:
  InstrumentationKey: ${APPINSIGHTS_KEY}
  EnableMetrics: true
  EnableTracing: true
  
  # Alert Rules
  Alerts:
    - name: HighErrorRate
      condition: ErrorRate > 5%
      action: Email
      recipients: admin@vitaguard.app
    
    - name: SlowResponse
      condition: AverageDuration > 2000ms
      action: Email
      recipients: admin@vitaguard.app
    
    - name: DatabaseDown
      condition: Availability < 99%
      action: Email,SMS
      recipients: admin@vitaguard.app, support@vitaguard.app

## API Security
APIEndpoints:
  # Require authentication for all endpoints
  DefaultAuthorization: Required
  
  # Public endpoints (no auth required)
  PublicEndpoints:
    - GET /health
    - GET /swagger
    - POST /api/auth/register
    - POST /api/auth/login
  
  # Admin-only endpoints
  AdminEndpoints:
    - GET /api/admin/users
    - GET /api/admin/logs
    - POST /api/admin/configuration
  
  # API Key validation
  APIKeyValidation: true
  APIKeyRotationSchedule: quarterly

## Third-Party Integrations
ThirdParty:
  # Email Service
  EmailService:
    Provider: Azure.Communication.Email
    EncryptedCredentials: true
    RateLimiting: 100/hour
  
  # SMS Service
  SMSService:
    Provider: Azure.Communication.Sms
    EncryptedCredentials: true
    RateLimiting: 50/hour
  
  # Location Service
  LocationService:
    Provider: Azure.Maps
    DataEncryption: true
    UserConsent: Required
    PrivacyPolicy: https://vitaguard.app/privacy

## GDPR & Privacy Compliance
GDPR:
  DataRetention:
    HealthRecords: 7  # years
    UserLogs: 2  # years
    DeleteUserDataAfterDays: 30
  
  UserRights:
    - DataExport
    - DataDeletion
    - DataCorrection
    - PrivacyPolicy
  
  ConsentTracking: true
  ConsentExpiration: 1  # years

## Infrastructure Security
Infrastructure:
  # Network
  Firewall: Enabled
  WAF: Azure Application Gateway
  DDoSProtection: Standard
  
  # Backup
  DatabaseBackup: Daily
  BackupRetention: 30  # days
  BackupEncryption: AES-256
  
  # Disaster Recovery
  DR:
    RPO: 1  # Recovery Point Objective (hours)
    RTO: 4  # Recovery Time Objective (hours)
    ReplicationRegion: West Europe
  
  # Audit
  AuditLogging: Enabled
  AuditRetention: 7  # years

## Incident Response
IncidentResponse:
  EscalationPath:
    - Level1: support@vitaguard.app
    - Level2: admin@vitaguard.app
    - Level3: security@vitaguard.app
  
  ResponseTime:
    Critical: 1  # hour
    High: 4  # hours
    Medium: 24  # hours
  
  PostIncident:
    - RootCauseAnalysis
    - SecurityPatch
    - StakeholderNotification
    - PolicyUpdate

## Compliance Checklist

### Before Production Deployment
- [ ] SSL/TLS Certificate installed (Let's Encrypt or Trusted CA)
- [ ] CORS policy configured for production domains
- [ ] Rate limiting enabled and tested
- [ ] Authentication & Authorization working
- [ ] Encryption enabled for sensitive data
- [ ] Logging configured (exclude sensitive data)
- [ ] Security headers configured
- [ ] Database backups configured
- [ ] Disaster recovery plan tested
- [ ] WAF rules configured
- [ ] DDoS protection enabled
- [ ] API keys rotated
- [ ] Passwords changed from defaults
- [ ] Firewall rules configured
- [ ] Monitoring & alerting enabled
- [ ] GDPR consent tracking enabled
- [ ] Privacy policy published
- [ ] Security audit completed
- [ ] Penetration testing completed
- [ ] Team training completed

### Post-Deployment Monitoring
- [ ] Daily security log review
- [ ] Weekly backup verification
- [ ] Monthly vulnerability scanning
- [ ] Quarterly penetration testing
- [ ] Annual compliance audit

---

**Last Updated:** 2024-01-22
**Status:** Production Ready
**Version:** 1.0.0
