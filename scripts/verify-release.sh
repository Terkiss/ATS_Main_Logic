#!/bin/bash
set -e

echo "======================================"
echo " Starting Official Release Gate"
echo "======================================"

echo "1. Cleaning solution..."
dotnet clean

echo "2. Building in Release mode..."
dotnet build -c Release

echo "3. Running tests in Release mode..."
dotnet test -c Release --no-build

echo "======================================"
echo " Release Gate Passed Successfully!"
echo "======================================"
