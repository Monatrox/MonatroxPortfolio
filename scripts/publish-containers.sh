#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")/.."
output="$PWD/artifacts/containers"
rm -rf -- "$output"
for pair in 'Blazor web' 'Api api' 'Migrations tools'; do
  read -r project directory <<< "$pair"
  dotnet publish "Hosts/MonatroxPortfolio.$project" -c Release --self-contained false \
    -p:UseAppHost=false -o "$output/$directory"
done
cp -R content "$output/tools/content"
