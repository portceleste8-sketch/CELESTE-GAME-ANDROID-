#!/usr/bin/env bash
set -Eeuo pipefail
IFS=$'\n\t'

ANDROID_API="34"
BUILD_TOOLS="34.0.0"
NDK_VERSION="26.3.11579264"
CMAKE_VERSION="3.22.1"

USER_HOME="$HOME"
if [ -n "${SUDO_USER:-}" ]; then
  USER_HOME=$(getent passwd "$SUDO_USER" | cut -d: -f6)
fi

ANDROID_SDK_DIR="$USER_HOME/android-sdk"
DOTNET_USER_DIR="$USER_HOME/.dotnet"
CMDLINE_TOOLS_URL="https://dl.google.com/android/repository/commandlinetools-linux-11076708_latest.zip"

# Flutter
FLUTTER_DIR="${FLUTTER_DIR:-$USER_HOME/flutter}"
FLUTTER_CHANNEL="${FLUTTER_CHANNEL:-stable}"
FLUTTER_PRECACHE_ANDROID="${FLUTTER_PRECACHE_ANDROID:-0}"   # 1 = baixa cache do engine p/ Android (pesado)
FLUTTER_RUN_DOCTOR="${FLUTTER_RUN_DOCTOR:-0}"               # 1 = roda flutter doctor no fim
FLUTTER_ACCEPT_ANDROID_LICENSES="${FLUTTER_ACCEPT_ANDROID_LICENSES:-1}" # 1 = tenta aceitar licenses via flutter

TIMESTAMP=$(date +%Y%m%d-%H%M%S)
LOG_FILE="$USER_HOME/bootstrap-install-$TIMESTAMP.log"

export DEBIAN_FRONTEND=noninteractive
exec > >(tee -a "$LOG_FILE") 2>&1

log_info()  { local msg="$1"; local time; time=$(date +%H:%M:%S); printf "\033[0;32m[%s] [INFO] %s\033[0m\n"  "$time" "$msg"; }
log_warn()  { local msg="$1"; local time; time=$(date +%H:%M:%S); printf "\033[0;33m[%s] [WARN] %s\033[0m\n"  "$time" "$msg" >&2; }
log_fatal() { local msg="$1"; local time; time=$(date +%H:%M:%S); printf "\033[0;31m[%s] [FATAL] %s\033[0m\n" "$time" "$msg" >&2; exit 1; }

on_error() {
  local exit_code=$?
  local line_number=${BASH_LINENO[0]}
  local command="${BASH_COMMAND}"
  log_fatal "Falha na linha $line_number executando: '$command'. Código: $exit_code. Log: $LOG_FILE"
}
trap on_error ERR

check_sudo() {
  if [ "$(id -u)" -ne 0 ] && ! command -v sudo >/dev/null 2>&1; then
    log_fatal "Este script requer root ou sudo instalado."
  fi
}

run_as_root() {
  if [ "$(id -u)" -eq 0 ]; then
    "$@"
  else
    sudo "$@"
  fi
}

run_as_user() {
  if [ -n "${SUDO_USER:-}" ]; then
    sudo -u "$SUDO_USER" "$@"
  else
    "$@"
  fi
}

append_to_profile() {
  local var_name="$1"
  local var_value="$2"
  local files=("$USER_HOME/.bashrc" "$USER_HOME/.profile" "$USER_HOME/.zshrc")

  export "$var_name"="$var_value"

  for file in "${files[@]}"; do
    if [ -f "$file" ] || [ "$file" = "$USER_HOME/.bashrc" ]; then
      touch "$file"
      if ! grep -q "export $var_name=" "$file"; then
        echo "export $var_name=\"$var_value\"" >> "$file"
        log_info "Variável $var_name adicionada em $file"
      fi
    fi
  done
}

append_to_path() {
  local new_path="$1"
  local files=("$USER_HOME/.bashrc" "$USER_HOME/.profile" "$USER_HOME/.zshrc")

  export PATH="$new_path:$PATH"

  for file in "${files[@]}"; do
    if [ -f "$file" ] || [ "$file" = "$USER_HOME/.bashrc" ]; then
      touch "$file"
      if ! grep -q "export PATH=\"$new_path:\$PATH\"" "$file"; then
        echo "export PATH=\"$new_path:\$PATH\"" >> "$file"
        log_info "PATH + $new_path em $file"
      fi
    fi
  done
}

install_apt_package() {
  local package="$1"
  if dpkg -s "$package" >/dev/null 2>&1; then
    log_info "Pacote '$package' já instalado."
  else
    log_info "Instalando pacote: $package"
    run_as_root apt-get install -y -qq \
      -o Dpkg::Options::="--force-confdef" \
      -o Dpkg::Options::="--force-confold" \
      --no-install-recommends "$package"
  fi
}

download_file() {
  local url="$1"
  local output="$2"
  local retries=3
  local count=0

  until [ "$count" -ge "$retries" ]; do
    log_info "Baixando $url (Tentativa $((count+1))/$retries)..."
    if curl -fsSL "$url" -o "$output"; then
      return 0
    fi
    count=$((count+1))
    log_warn "Falha no download. Tentando de novo em 2s..."
    sleep 2
  done
  log_fatal "Falha ao baixar $url após $retries tentativas."
}

phase_system_prep() {
  log_info "Atualizando apt..."
  run_as_root apt-get update -y -qq

  log_info "Instalando dependências essenciais..."
  local basic_packages=(
    "bash"
    "coreutils"
    "git"
    "curl"
    "wget"
    "unzip"
    "zip"
    "tar"
    "xz-utils"
    "software-properties-common"
    "build-essential"
    "gcc"
    "g++"
    "make"
    "cmake"
    "ninja-build"
    "pkg-config"
    "autoconf"
    "libtool"
    "clang"
    "llvm"
    "lld"
    "gdb"
    "python3"
    "python3-pip"
    "libsdl2-dev"
    "libopenal-dev"
    "libfreetype6-dev"
    "libfontconfig1-dev"
    "libssl-dev"
    "zlib1g-dev"
    # úteis p/ Flutter/Android tooling
    "ca-certificates"
    "gnupg"
  )

  for pkg in "${basic_packages[@]}"; do
    install_apt_package "$pkg"
  done
}

phase_nodejs() {
  log_info "Verificando Node.js..."
  local need_install=true
  if command -v node >/dev/null 2>&1; then
    local node_version
    node_version=$(node -v)
    if [[ "$node_version" == v20* ]]; then
      log_info "Node.js $node_version já instalado."
      need_install=false
    fi
  fi

  if [ "$need_install" = true ]; then
    log_info "Configurando NodeSource v20..."
    run_as_root mkdir -p /etc/apt/keyrings
    run_as_root rm -f /etc/apt/keyrings/nodesource.gpg

    curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | \
      run_as_root gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg

    echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | \
      run_as_root tee /etc/apt/sources.list.d/nodesource.list >/dev/null

    run_as_root apt-get update -y -qq
    install_apt_package "nodejs"
  fi
}

phase_java() {
  log_info "Instalando Java 17..."
  install_apt_package "openjdk-17-jdk"

  local javac_path
  javac_path=$(readlink -f "$(command -v javac)" 2>/dev/null || true)

  if [ -n "$javac_path" ]; then
    local java_home_dir
    java_home_dir=$(echo "$javac_path" | sed 's#/bin/javac##')
    log_info "JAVA_HOME: $java_home_dir"
    append_to_profile "JAVA_HOME" "$java_home_dir"
    append_to_path "$java_home_dir/bin"
  else
    log_warn "Não detectei javac automaticamente."
  fi
}

phase_dotnet() {
  log_info "Verificando .NET 8..."
  if command -v dotnet >/dev/null 2>&1 && [[ "$(dotnet --version)" == 8.* ]]; then
    log_info ".NET 8 já instalado."
  else
    log_info "Instalando .NET 8 (dotnet-install.sh)..."
    local install_script="/tmp/dotnet-install.sh"
    download_file "https://dot.net/v1/dotnet-install.sh" "$install_script"
    chmod +x "$install_script"

    run_as_user mkdir -p "$DOTNET_USER_DIR"
    run_as_user bash "$install_script" --channel 8.0 --install-dir "$DOTNET_USER_DIR"

    append_to_profile "DOTNET_ROOT" "$DOTNET_USER_DIR"
    append_to_path "$DOTNET_USER_DIR"
    append_to_path "$DOTNET_USER_DIR/tools"

    rm -f "$install_script"
  fi

  log_info "Verificando Workload Android (.NET)..."
  if ! run_as_user "$DOTNET_USER_DIR/dotnet" workload list | grep -qi "android"; then
    log_info "Instalando Workload Android..."
    run_as_user "$DOTNET_USER_DIR/dotnet" workload install android --skip-manifest-update
  else
    log_info "Workload Android já instalado."
  fi
}

phase_android_sdk() {
  log_info "Configurando Android SDK em: $ANDROID_SDK_DIR"

  run_as_user mkdir -p "$ANDROID_SDK_DIR/cmdline-tools"
  run_as_user mkdir -p "$ANDROID_SDK_DIR/platform-tools"
  run_as_user mkdir -p "$USER_HOME/.android"
  run_as_user touch "$USER_HOME/.android/repositories.cfg"

  if [ ! -d "$ANDROID_SDK_DIR/cmdline-tools/latest" ]; then
    log_info "Baixando Android Command Line Tools..."
    local zip_path="/tmp/cmdline-tools.zip"
    download_file "$CMDLINE_TOOLS_URL" "$zip_path"

    local temp_extract="/tmp/cmdline-extract"
    mkdir -p "$temp_extract"
    unzip -q "$zip_path" -d "$temp_extract"

    run_as_user mkdir -p "$ANDROID_SDK_DIR/cmdline-tools/latest"

    if [ -d "$temp_extract/cmdline-tools" ]; then
      run_as_user cp -r "$temp_extract/cmdline-tools/"* "$ANDROID_SDK_DIR/cmdline-tools/latest/"
    elif [ -d "$temp_extract/tools" ]; then
      run_as_user cp -r "$temp_extract/tools/"* "$ANDROID_SDK_DIR/cmdline-tools/latest/"
    else
      run_as_user cp -r "$temp_extract/"* "$ANDROID_SDK_DIR/cmdline-tools/latest/"
    fi

    rm -rf "$temp_extract"
    rm -f "$zip_path"
  fi

  append_to_profile "ANDROID_HOME" "$ANDROID_SDK_DIR"
  append_to_profile "ANDROID_SDK_ROOT" "$ANDROID_SDK_DIR"
  append_to_path "$ANDROID_SDK_DIR/cmdline-tools/latest/bin"
  append_to_path "$ANDROID_SDK_DIR/platform-tools"

  log_info "Escrevendo licenças do Android SDK..."
  run_as_user mkdir -p "$ANDROID_SDK_DIR/licenses"
  echo "24333f8a63b6825ea9c5514f83c2829b004d1fee" | run_as_user tee "$ANDROID_SDK_DIR/licenses/android-sdk-license" >/dev/null
  echo "84831b9409646a918e30573bab4c9c91346d8abd" | run_as_user tee -a "$ANDROID_SDK_DIR/licenses/android-sdk-license" >/dev/null
  echo "601085b94cd77f0b54ff86406957099ebe79c4d6" | run_as_user tee "$ANDROID_SDK_DIR/licenses/android-googletv-license" >/dev/null
  echo "33b6a2b64607f11b759f320ef9dff4ae5c47d97a" | run_as_user tee "$ANDROID_SDK_DIR/licenses/google-gdk-license" >/dev/null
  echo "e9acab5b5fbb560a72cfaecce6eb5b36b1e850d9" | run_as_user tee "$ANDROID_SDK_DIR/licenses/mips-android-sysimage-license" >/dev/null

  local sdkmanager_bin="$ANDROID_SDK_DIR/cmdline-tools/latest/bin/sdkmanager"

  log_info "Instalando Android packages (platform/build-tools/ndk/cmake)..."
  yes | run_as_user "$sdkmanager_bin" --install \
    "platform-tools" \
    "platforms;android-${ANDROID_API}" \
    "build-tools;${BUILD_TOOLS}" \
    "ndk;${NDK_VERSION}" \
    "cmake;${CMAKE_VERSION}" >/dev/null
}

# Flutter (não-interativo, SDK oficial)
phase_flutter() {
  log_info "Verificando Flutter..."

  # deps comuns (evita erro de libs em várias distros)
  local flutter_deps=(
    "xz-utils"
    "zip"
    "unzip"
    "git"
    "libglu1-mesa"
    "libgl1"
    "libgtk-3-0"
    "libnss3"
    "libxss1"
    "libasound2"
  )
  for pkg in "${flutter_deps[@]}"; do
    install_apt_package "$pkg"
  done

  # se já existe flutter bin
  if [ -x "$FLUTTER_DIR/bin/flutter" ]; then
    log_info "Flutter já presente em: $FLUTTER_DIR"
  else
    log_info "Baixando Flutter ($FLUTTER_CHANNEL) e instalando em: $FLUTTER_DIR"

    local releases_json="/tmp/flutter_releases.json"
    download_file "https://storage.googleapis.com/flutter_infra_release/releases/releases_linux.json" "$releases_json"

    local flutter_url
    flutter_url=$(python3 - <<'PY'
import json, sys
p = "/tmp/flutter_releases.json"
data = json.load(open(p, "r", encoding="utf-8"))
base = data.get("base_url","").rstrip("/")
chan = "stable"
cur = data.get("current_release",{}).get(chan)
if not base or not cur:
    raise SystemExit(2)
arch = None
for r in data.get("releases", []):
    if r.get("hash") == cur:
        arch = r.get("archive")
        break
if not arch:
    raise SystemExit(3)
print(f"{base}/{arch}")
PY
)
    if [ -z "${flutter_url:-}" ]; then
      log_fatal "Não consegui resolver a URL do Flutter."
    fi

    local tar_path="/tmp/flutter_sdk.tar.xz"
    download_file "$flutter_url" "$tar_path"

    # remove instalação antiga parcial, se houver
    run_as_root rm -rf "$FLUTTER_DIR" || true

    # extrai para o $USER_HOME (o tar geralmente cria a pasta 'flutter/')
    run_as_user mkdir -p "$USER_HOME"
    run_as_user tar -xf "$tar_path" -C "$USER_HOME"

    # se extração criou $USER_HOME/flutter, normaliza para FLUTTER_DIR se diferente
    if [ "$FLUTTER_DIR" != "$USER_HOME/flutter" ]; then
      if [ -d "$USER_HOME/flutter" ]; then
        run_as_root rm -rf "$FLUTTER_DIR" || true
        run_as_root mv "$USER_HOME/flutter" "$FLUTTER_DIR"
        run_as_root chown -R "${SUDO_USER:-$(id -un)}":"${SUDO_USER:-$(id -un)}" "$FLUTTER_DIR" || true
      fi
    fi

    rm -f "$tar_path" "$releases_json"
  fi

  # PATH persistente
  append_to_profile "FLUTTER_HOME" "$FLUTTER_DIR"
  append_to_path "$FLUTTER_DIR/bin"

  # garante ANDROID_SDK_ROOT no ambiente do flutter
  export ANDROID_SDK_ROOT="$ANDROID_SDK_DIR"
  export ANDROID_HOME="$ANDROID_SDK_DIR"
  export PATH="$ANDROID_SDK_DIR/platform-tools:$ANDROID_SDK_DIR/cmdline-tools/latest/bin:$PATH"

  # inicializa sem prompts (doctor pode criar cache)
  log_info "Flutter version:"
  run_as_user "$FLUTTER_DIR/bin/flutter" --version || true

  # aceita licenses via flutter (não-interativo)
  if [ "$FLUTTER_ACCEPT_ANDROID_LICENSES" = "1" ]; then
    log_info "Tentando aceitar android licenses via flutter (não-interativo)..."
    yes | run_as_user "$FLUTTER_DIR/bin/flutter" doctor --android-licenses >/dev/null 2>&1 || true
  fi

  # opcional: precache Android (pesado)
  if [ "$FLUTTER_PRECACHE_ANDROID" = "1" ]; then
    log_info "Rodando flutter precache --android (pode ser pesado)..."
    run_as_user "$FLUTTER_DIR/bin/flutter" precache --android >/dev/null 2>&1 || true
  fi
}

phase_monogame() {
  log_info "Instalando Mono..."
  install_apt_package "mono-complete"

  log_info "Instalando templates MonoGame e MGCB Editor..."
  run_as_user "$DOTNET_USER_DIR/dotnet" new install MonoGame.Templates.CSharp >/dev/null 2>&1 || true
  run_as_user "$DOTNET_USER_DIR/dotnet" tool install --global dotnet-mgcb-editor >/dev/null 2>&1 || true
}

phase_docker() {
  log_info "Verificando Docker..."
  if ! command -v docker >/dev/null 2>&1; then
    log_info "Instalando Docker..."
    local docker_script="/tmp/get-docker.sh"
    download_file "https://get.docker.com" "$docker_script"
    run_as_root sh "$docker_script"
    rm -f "$docker_script"

    if [ -n "${SUDO_USER:-}" ]; then
      log_info "Adicionando $SUDO_USER ao grupo docker..."
      run_as_root usermod -aG docker "$SUDO_USER" || true
    fi
  else
    log_info "Docker já instalado."
  fi
}

phase_vscode() {
  log_info "Verificando VS Code..."
  if ! command -v code >/dev/null 2>&1; then
    log_info "Instalando VS Code..."
    install_apt_package "apt-transport-https"
    install_apt_package "gpg"
    install_apt_package "wget"

    local key_ring="/etc/apt/keyrings/packages.microsoft.gpg"
    wget -qO- https://packages.microsoft.com/keys/microsoft.asc | gpg --dearmor | run_as_root tee "$key_ring" >/dev/null

    echo "deb [arch=amd64,arm64,armhf signed-by=$key_ring] https://packages.microsoft.com/repos/code stable main" | \
      run_as_root tee /etc/apt/sources.list.d/vscode.list >/dev/null

    run_as_root apt-get update -y -qq
    install_apt_package "code"
  else
    log_info "VS Code já instalado."
  fi

  log_info "Instalando extensões do VS Code..."
  local extensions=(
    "13xforever.language-x86-64-assembly"
    "adelphes.android-dev-ext"
    "bbenoist.doxygen"
    "cheshirekow.cmake-format"
    "chrisatwindsurf.csharpextension"
    "chrisgroks.csharpextension"
    "cschlosser.doxdocgen"
    "danielpinto8zz6.c-cpp-project-generator"
    "dart-code.dart-code"
    "dart-code.flutter"
    "diemasmichiels.emulate"
    "dotnetdev-kr-custom.csharp"
    "dr-mohammed-hamed.android-studio-flash"
    "editorconfig.editorconfig"
    "franneck94.c-cpp-runner"
    "franneck94.vscode-c-cpp-config"
    "franneck94.vscode-c-cpp-dev-extension-pack"
    "haloscript.astyle-lsp-vscode"
    "hanwang.android-adb-wlan"
    "jajera.vsx-remote-ssh"
    "jeff-hykin.better-cpp-syntax"
    "jnoortheen.nix-ide"
    "kylinideteam.cmake-intellisence"
    "kylinideteam.cppdebug"
    "kylinideteam.kylin-clangd"
    "kylinideteam.kylin-cmake-tools"
    "kylinideteam.kylin-cpp-pack"
    "llvm-vs-code-extensions.vscode-clangd"
    "mitaki28.vscode-clang"
    "ms-dotnettools.csharp"
    "ms-dotnettools.csdevkit"
    "ms-dotnettools.vscode-dotnet-runtime"
    "ms-vscode.cmake-tools"
    "ms-vscode.cpptools"
    "muhammad-sammy.csharp"
    "november.clover-unity"
    "oracle.oracle-java"
    "redhat.java"
    "redhat.vscode-yaml"
    "twxs.cmake"
    "vadimcn.vscode-lldb"
    "zlorn.vstuc"
  )

  local installed_list=""
  if command -v code >/dev/null 2>&1; then
    if [ -n "${SUDO_USER:-}" ]; then
      installed_list=$(run_as_user code --list-extensions || true)
    else
      installed_list=$(code --list-extensions || true)
    fi
    installed_list=$(echo "$installed_list" | tr '[:upper:]' '[:lower:]')
  fi

  local count=0
  local total=${#extensions[@]}

  for ext in "${extensions[@]}"; do
    count=$((count + 1))
    local ext_lower
    ext_lower=$(echo "$ext" | tr '[:upper:]' '[:lower:]')

    if echo "$installed_list" | grep -q "$ext_lower"; then
      log_info "[$count/$total] Já instalada: $ext"
    else
      log_info "[$count/$total] Instalando: $ext"
      if [ -n "${SUDO_USER:-}" ]; then
        sudo -u "$SUDO_USER" code --install-extension "$ext" --force >/dev/null 2>&1 || log_warn "Falha: $ext"
      else
        code --install-extension "$ext" --force >/dev/null 2>&1 || log_warn "Falha: $ext"
      fi
    fi
  done
}

phase_cleanup() {
  log_info "Limpando apt e temporários..."
  run_as_root apt-get clean
  run_as_root rm -rf /var/lib/apt/lists/*
  run_as_root rm -rf /tmp/*dotnet* /tmp/*android* /tmp/flutter_* /tmp/flutter*
}

phase_report() {
  log_info "Relatório final:"
  echo ""
  echo "RELATÓRIO DE AMBIENTE INSTALADO"
  echo "Data: $(date)"
  echo ""
  echo "--- Sistema ---"
  echo "OS: $(grep PRETTY_NAME /etc/os-release | cut -d= -f2 | tr -d '\"')"
  echo "Kernel: $(uname -r)"
  echo ""
  echo "--- Linguagens & Runtimes ---"
  echo "Java: $(java -version 2>&1 | head -n 1)"
  echo "Node.js: $(run_as_user node -v 2>/dev/null || echo 'Erro')"
  echo "NPM: $(run_as_user npm -v 2>/dev/null || echo 'Erro')"
  echo ".NET SDK: $(run_as_user "$DOTNET_USER_DIR/dotnet" --version 2>/dev/null || echo 'Erro')"
  echo "Mono: $(mono --version 2>/dev/null | head -n 1 || echo 'Não encontrado')"
  echo "Flutter: $(run_as_user "$FLUTTER_DIR/bin/flutter" --version 2>/dev/null | head -n 1 || echo 'Não encontrado')"
  echo ""
  echo "--- Android ---"
  echo "SDK Root: $ANDROID_SDK_DIR"
  echo "ADB: $(run_as_user "$ANDROID_SDK_DIR/platform-tools/adb" version 2>/dev/null | head -n 1 || echo 'Erro')"
  echo "NDK Version: $NDK_VERSION"
  echo ""
  echo "--- Ferramentas ---"
  echo "Docker: $(docker --version 2>/dev/null || echo 'Não encontrado')"
  echo "Docker Compose: $(docker compose version 2>/dev/null || echo 'Não encontrado')"
  echo "VS Code: $(code --version 2>/dev/null | head -n 1 || echo 'Não encontrado')"
  echo ""
  echo "Concluído."
  echo "Reabra o terminal (ou 'source ~/.bashrc') para carregar variáveis."
  echo "Se instalou Docker, pode precisar logout/login para grupo docker."
  echo ""
}

main() {
  log_info "Iniciando bootstrap (não-interativo)..."
  check_sudo

  phase_system_prep
  phase_nodejs
  phase_java
  phase_dotnet
  phase_android_sdk
  phase_flutter
  phase_monogame
  phase_docker
  phase_vscode
  phase_cleanup

  if [ "$FLUTTER_RUN_DOCTOR" = "1" ]; then
    log_info "Executando flutter doctor (não-interativo)..."
    run_as_user "$FLUTTER_DIR/bin/flutter" doctor || true
  fi

  phase_report
  log_info "Log completo: $LOG_FILE"
}

main "$@"
