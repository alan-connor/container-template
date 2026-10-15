<#
.SYNOPSIS
    Deploys the container-template image (from GitHub Container Registry) to a local
    Rancher Desktop (k3s) cluster via Helm.

.DESCRIPTION
    Wraps the namespace + optional ghcr pull-secret + helm upgrade steps into one command.
    Requires: Rancher Desktop running, kubectl context = rancher-desktop, helm installed.

.PARAMETER Tag
    Image tag to deploy. Defaults to 'latest'. Use e.g. 'main-<sha>' for an immutable build.

.PARAMETER GhcrPat
    GitHub Personal Access Token with 'read:packages'. Only needed if the ghcr package is
    PRIVATE. When supplied, an image pull secret named 'ghcr-pull' is created/updated and
    wired into the release.

.PARAMETER Namespace
    Target namespace. Defaults to 'container-template'.

.PARAMETER Uninstall
    Removes the release and namespace instead of deploying.

.PARAMETER Force
    Deploy even when the current kubectl context is not 'rancher-desktop'.

.EXAMPLE
    .\deploy-local.ps1
    Deploy the 'latest' public image.

.EXAMPLE
    .\deploy-local.ps1 -Tag main-abc1234 -GhcrPat $env:GHCR_PAT
    Deploy a specific build from a private package.

.EXAMPLE
    .\deploy-local.ps1 -Uninstall
#>
[CmdletBinding()]
param(
    [string]$Tag = "latest",
    [string]$GhcrPat = "",
    [string]$Namespace = "container-template",
    [switch]$Uninstall,
    [switch]$Force
)

$ErrorActionPreference = "Stop"
$ReleaseName = "container-template"
$ChartPath   = Join-Path $PSScriptRoot "helm"
$GhcrUser    = "alan-connor"
$SecretName  = "ghcr-pull"

function Assert-Tool($name) {
    if (-not (Get-Command $name -ErrorAction SilentlyContinue)) {
        throw "'$name' was not found on PATH. Is Rancher Desktop installed and running?"
    }
}

Assert-Tool kubectl
Assert-Tool helm

# Confirm we're pointed at the local cluster, not something remote.
$ctx = (kubectl config current-context).Trim()
Write-Host "kubectl context: $ctx" -ForegroundColor Cyan
if ($ctx -ne "rancher-desktop") {
    if (-not $Force) {
        throw "Current context is '$ctx', not 'rancher-desktop'. Switch with 'kubectl config use-context rancher-desktop', or pass -Force to deploy anyway."
    }
    Write-Warning "Current context is '$ctx', not 'rancher-desktop'. Continuing because -Force was passed."
}

if ($Uninstall) {
    Write-Host "Uninstalling release '$ReleaseName' from namespace '$Namespace'..." -ForegroundColor Yellow
    helm uninstall $ReleaseName --namespace $Namespace
    kubectl delete namespace $Namespace --ignore-not-found
    Write-Host "Done." -ForegroundColor Green
    return
}

# Ensure namespace exists (idempotent).
kubectl get namespace $Namespace *> $null
if ($LASTEXITCODE -ne 0) {
    kubectl create namespace $Namespace | Out-Null
    Write-Host "Created namespace '$Namespace'." -ForegroundColor Green
}

# Build helm --set args.
$setArgs = @("--set", "image.tag=$Tag")

# Private package: create/refresh the pull secret and wire it in.
if ($GhcrPat) {
    Write-Host "Creating/updating ghcr pull secret '$SecretName'..." -ForegroundColor Cyan
    kubectl create secret docker-registry $SecretName `
        --namespace $Namespace `
        --docker-server=ghcr.io `
        --docker-username=$GhcrUser `
        --docker-password=$GhcrPat `
        --dry-run=client -o yaml | kubectl apply -f -
    $setArgs += @("--set", "imagePullSecrets[0].name=$SecretName")
}

Write-Host "Deploying '$ReleaseName' (tag: $Tag) to namespace '$Namespace'..." -ForegroundColor Cyan
helm upgrade --install $ReleaseName $ChartPath --namespace $Namespace @setArgs

Write-Host "`nWaiting for rollout..." -ForegroundColor Cyan
kubectl rollout status deploy/$ReleaseName --namespace $Namespace --timeout=120s

$ingressHost = (helm get values $ReleaseName --namespace $Namespace -a -o json | ConvertFrom-Json).ingress.host
Write-Host "`nDeployed." -ForegroundColor Green
Write-Host "  Ingress:  http://$ingressHost/api/hello   (Swagger: http://$ingressHost/swagger)" -ForegroundColor Green
Write-Host "  Or port-forward: kubectl port-forward -n $Namespace svc/$ReleaseName 8080:80" -ForegroundColor Green
