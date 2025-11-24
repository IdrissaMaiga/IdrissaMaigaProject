# kubectl wrapper for WSL Minikube
# This script makes it easy to run kubectl commands that work with WSL Minikube

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$Arguments
)

# Run kubectl command in WSL
$command = "kubectl " + ($Arguments -join " ")
wsl -d Ubuntu-24.04 -e bash -c $command

