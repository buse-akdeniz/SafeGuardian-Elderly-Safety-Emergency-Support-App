#!/bin/bash

# Setup Verification Script
# Otomatik backend + web sync yapılandırmasının doğru kurulu olduğunu kontrol eder

echo "🔍 SafeGuardian Dev Server Setup Verification"
echo "=============================================="
echo ""

# 1. Check chokidar-cli
echo "1️⃣  Checking chokidar-cli..."
if cd "AsistanApp" && npx chokidar --version > /dev/null 2>&1; then
    VERSION=$(npx chokidar --version 2>&1 | grep "chokidar-cli" | cut -d: -f2 | xargs)
    echo "   ✅ chokidar-cli $VERSION installed"
else
    echo "   ❌ chokidar-cli NOT found"
    echo "   🔧 Run: npm install --save-dev chokidar-cli"
    EXIT_CODE=1
fi
cd - > /dev/null 2>&1

# 2. Check tasks.json
echo ""
echo "2️⃣  Checking .vscode/tasks.json..."
if [ -f ".vscode/tasks.json" ]; then
    if python3 -c "import json; json.load(open('.vscode/tasks.json'))" 2>/dev/null; then
        if grep -q '"backend-watch"' ".vscode/tasks.json" && \
           grep -q '"web-sync-watch"' ".vscode/tasks.json" && \
           grep -q '"dev-server"' ".vscode/tasks.json"; then
            echo "   ✅ tasks.json is valid with all 3 tasks defined"
        else
            echo "   ⚠️  tasks.json exists but missing some tasks"
        fi
    else
        echo "   ❌ tasks.json has JSON syntax error"
        EXIT_CODE=1
    fi
else
    echo "   ❌ tasks.json NOT found"
    EXIT_CODE=1
fi

# 3. Check launch.json
echo ""
echo "3️⃣  Checking .vscode/launch.json..."
if [ -f ".vscode/launch.json" ]; then
    if python3 -c "import json; json.load(open('.vscode/launch.json'))" 2>/dev/null; then
        echo "   ✅ launch.json is valid"
    else
        echo "   ⚠️  launch.json has JSON syntax error (not critical)"
    fi
else
    echo "   ⚠️  launch.json NOT found (optional)"
fi

# 4. Check package.json scripts
echo ""
echo "4️⃣  Checking AsistanApp/package.json..."
if cd "AsistanApp" && grep -q '"cap:copy:watch"' "package.json"; then
    echo "   ✅ cap:copy:watch script defined in package.json"
else
    echo "   ❌ cap:copy:watch script NOT found in package.json"
    EXIT_CODE=1
fi
cd - > /dev/null 2>&1

# 5. Check node_modules for chokidar
echo ""
echo "5️⃣  Checking node_modules..."
if [ -d "AsistanApp/node_modules/chokidar-cli" ]; then
    echo "   ✅ chokidar-cli installed in node_modules"
else
    echo "   ❌ chokidar-cli NOT in node_modules"
    echo "   🔧 Run: cd AsistanApp && npm install"
    EXIT_CODE=1
fi

# 6. Check .NET availability
echo ""
echo "6️⃣  Checking dotnet..."
if command -v dotnet &> /dev/null; then
    VERSION=$(dotnet --version)
    echo "   ✅ dotnet $VERSION available"
else
    echo "   ❌ dotnet NOT in PATH"
    EXIT_CODE=1
fi

# 7. Backend port test
echo ""
echo "7️⃣  Testing backend port 5007..."
if curl -s -o /dev/null -w "%{http_code}" http://127.0.0.1:5007/ 2>/dev/null | grep -q "200"; then
    echo "   ✅ Backend appears to be running"
else
    echo "   ⓘ  Backend not currently running (this is OK, will start via task)"
fi

# Summary
echo ""
echo "=============================================="
if [ -z "$EXIT_CODE" ]; then
    echo "✅ All checks passed! Ready to run dev-server"
    echo ""
    echo "🎯 Next step:"
    echo "   Press Cmd+Shift+P (Mac) or Ctrl+Shift+P (Windows/Linux)"
    echo "   Type: Tasks: Run Task"
    echo "   Select: dev-server"
else
    echo "❌ Some checks failed. See above for details."
fi
