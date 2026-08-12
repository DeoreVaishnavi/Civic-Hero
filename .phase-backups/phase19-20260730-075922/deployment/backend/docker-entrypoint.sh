#!/bin/sh
set -eu

load_secret() {
  variable_name="$1"
  file_variable_name="${variable_name}_FILE"
  eval "file_path=\${${file_variable_name}:-}"

  if [ -n "${file_path}" ]; then
    if [ ! -r "${file_path}" ]; then
      echo "CivicHero secret file is missing or unreadable: ${file_path}" >&2
      exit 1
    fi

    value="$(cat "${file_path}")"
    if [ -n "${value}" ]; then
      export "${variable_name}=${value}"
    fi
  fi
}

load_secret "ConnectionStrings__DefaultConnection"
load_secret "Jwt__SecretKey"
load_secret "AWS__AccessKey"
load_secret "AWS__SecretKey"
load_secret "AI__ApiKey"
load_secret "BootstrapSuperAdmin__Password"

exec dotnet CivicHero.Backend.dll
