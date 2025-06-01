# Clubber Tailwind Setup Script
# Run this in your Clubber.Web folder

Write-Host "🎨 Setting up Tailwind CSS for Clubber..." -ForegroundColor Cyan

# Check if Node.js is installed
try {
    $nodeVersion = node --version
    Write-Host "✅ Node.js found: $nodeVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ Node.js not found. Please install Node.js from https://nodejs.org/" -ForegroundColor Red
    Write-Host "📥 Download the LTS version and restart your terminal after installation." -ForegroundColor Yellow
    exit 1
}

# Check if npm is working
try {
    $npmVersion = npm --version
    Write-Host "✅ npm found: $npmVersion" -ForegroundColor Green
} catch {
    Write-Host "❌ npm not found. Please restart your terminal after installing Node.js." -ForegroundColor Red
    exit 1
}

# Create Styles folder if it doesn't exist
if (!(Test-Path "Styles")) {
    New-Item -ItemType Directory -Name "Styles"
    Write-Host "✅ Created Styles folder" -ForegroundColor Green
}

# Create wwwroot/css folder if it doesn't exist
if (!(Test-Path "wwwroot/css")) {
    New-Item -ItemType Directory -Path "wwwroot/css" -Force
    Write-Host "✅ Created wwwroot/css folder" -ForegroundColor Green
}

# Install Tailwind CSS
Write-Host "📦 Installing Tailwind CSS and dependencies..." -ForegroundColor Yellow
npm install

# Initialize Tailwind (create config if it doesn't exist)
if (!(Test-Path "tailwind.config.js")) {
    Write-Host "🔧 Initializing Tailwind config..." -ForegroundColor Yellow
    npx tailwindcss init
}

# Build CSS for the first time
Write-Host "🔨 Building CSS for the first time..." -ForegroundColor Yellow
npx tailwindcss -i ./Styles/input.css -o ./wwwroot/css/site.css --minify

Write-Host ""
Write-Host "🎉 Tailwind CSS setup complete!" -ForegroundColor Green
Write-Host ""
Write-Host "📝 Next steps:" -ForegroundColor Cyan
Write-Host "1. For development with auto-rebuild: npm run build-css" -ForegroundColor White
Write-Host "2. For production build: npm run build-css-prod" -ForegroundColor White
Write-Host "3. Your site.css is now ready in wwwroot/css/" -ForegroundColor White
Write-Host ""
Write-Host "🚀 You can now run your .NET application!" -ForegroundColor Green
