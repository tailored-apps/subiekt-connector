#!/bin/bash
# 🐧 Instalacja Git hooks dla Subiekt Connector
# Uruchom raz po sklonowaniu repo: ./setup-hooks.sh

set -e

echo "🐧 Instalacja Git hooks..."
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit
chmod +x .githooks/pre-push

echo "✅ Git hooks zainstalowane:"
echo "   pre-commit  → dotnet format --verify-no-changes"
echo "   pre-push    → dotnet build + dotnet test"
echo ""
echo "Aby pominąć hook (awaryjnie): git commit --no-verify"
