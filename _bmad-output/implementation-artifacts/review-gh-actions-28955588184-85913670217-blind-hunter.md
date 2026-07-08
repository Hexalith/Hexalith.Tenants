# Blind Hunter Review Prompt

Use the `bmad-review-adversarial-general` skill.

You receive inline diff only. Do not read the repository, do not ask for context,
and do not use project files. Review skeptically for bugs, regressions, missing
safeguards, and review risks. Return concise findings only, with file/hunk
references where possible.

## Diff Payload

```diff
diff --git a/references/Hexalith.Builds b/references/Hexalith.Builds
--- a/references/Hexalith.Builds
+++ b/references/Hexalith.Builds
@@ submodule pointer @@
-Subproject commit 890553bd638b8ecba769555b81f81d80538dae25
+Subproject commit ea1d02f8d0b3a34f6039262549b807b1e12729f3

Submodule commit diff 890553b..ea1d02f:
diff --git a/.github/workflows/domain-ci.yml b/.github/workflows/domain-ci.yml
index c6c7927..b09dcdd 100644
--- a/.github/workflows/domain-ci.yml
+++ b/.github/workflows/domain-ci.yml
@@ -143,37 +143,11 @@ jobs:
               --results-directory "TestResults/${name}" \
               --collect:"XPlat Code Coverage"
           done <<< "$UNIT_TEST_PROJECTS"
-      - name: Install Dapr CLI
+      - name: Install and initialize Dapr
         if: ${{ inputs.integration-test-projects != '' }}
-        shell: bash
-        env:
-          DAPR_VERSION: ${{ inputs.dapr-version }}
-        run: |
-          set -euo pipefail
-          version="${DAPR_VERSION#v}"
-          os="$(uname | tr '[:upper:]' '[:lower:]')"
-          case "$(uname -m)" in
-            x86_64) arch="amd64" ;;
-            aarch64) arch="arm64" ;;
-            armv7*) arch="arm" ;;
-            *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
-          esac
-          install_dir="${RUNNER_TEMP}/dapr-cli"
-          mkdir -p "$install_dir"
-          archive="${RUNNER_TEMP}/dapr-cli.tar.gz"
-          curl -fsSL "https://github.com/dapr/cli/releases/download/v${version}/dapr_${os}_${arch}.tar.gz" -o "$archive"
-          tar -xzf "$archive" -C "$install_dir" dapr
-          chmod +x "${install_dir}/dapr"
-          echo "$install_dir" >> "$GITHUB_PATH"
-          "${install_dir}/dapr" --version
-      - name: Run dapr init
-        if: ${{ inputs.integration-test-projects != '' }}
-        uses: nick-fields/retry@ad984534de44a9489a53aefd81eb77f87c70dc60 # v4.0.0
+        uses: Hexalith/Hexalith.Builds/Github/dapr-init@main
         with:
-          timeout_minutes: 5
-          max_attempts: 3
-          retry_wait_seconds: 15
-          command: dapr init
+          version: ${{ inputs.dapr-version }}
@@ -242,35 +216,10 @@ jobs:
         run: |
           dotnet restore ${{ inputs.solution }}
           dotnet build ${{ inputs.solution }} --no-restore --configuration Release -warnaserror
-      - name: Install Dapr CLI
-        shell: bash
-        env:
-          DAPR_VERSION: ${{ inputs.dapr-version }}
-        run: |
-          set -euo pipefail
-          version="${DAPR_VERSION#v}"
-          os="$(uname | tr '[:upper:]' '[:lower:]')"
-          case "$(uname -m)" in
-            x86_64) arch="amd64" ;;
-            aarch64) arch="arm64" ;;
-            armv7*) arch="arm" ;;
-            *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
-          esac
-          install_dir="${RUNNER_TEMP}/dapr-cli"
-          mkdir -p "$install_dir"
-          archive="${RUNNER_TEMP}/dapr-cli.tar.gz"
-          curl -fsSL "https://github.com/dapr/cli/releases/download/v${version}/dapr_${os}_${arch}.tar.gz" -o "$archive"
-          tar -xzf "$archive" -C "$install_dir" dapr
-          chmod +x "${install_dir}/dapr"
-          echo "$install_dir" >> "$GITHUB_PATH"
-          "${install_dir}/dapr" --version
-      - name: Run dapr init
-        uses: nick-fields/retry@ad984534de44a9489a53aefd81eb77f87c70dc60 # v4.0.0
+      - name: Install and initialize Dapr
+        uses: Hexalith/Hexalith.Builds/Github/dapr-init@main
         with:
-          timeout_minutes: 5
-          max_attempts: 3
-          retry_wait_seconds: 15
-          command: dapr init
+          version: ${{ inputs.dapr-version }}
@@ -322,35 +271,10 @@ jobs:
         run: |
           dotnet restore ${{ inputs.solution }}
           dotnet build ${{ inputs.solution }} --no-restore --configuration Release -warnaserror
-      - name: Install Dapr CLI
-        shell: bash
-        env:
-          DAPR_VERSION: ${{ inputs.dapr-version }}
-        run: |
-          set -euo pipefail
-          version="${DAPR_VERSION#v}"
-          os="$(uname | tr '[:upper:]' '[:lower:]')"
-          case "$(uname -m)" in
-            x86_64) arch="amd64" ;;
-            aarch64) arch="arm64" ;;
-            armv7*) arch="arm" ;;
-            *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
-          esac
-          install_dir="${RUNNER_TEMP}/dapr-cli"
-          mkdir -p "$install_dir"
-          archive="${RUNNER_TEMP}/dapr-cli.tar.gz"
-          curl -fsSL "https://github.com/dapr/cli/releases/download/v${version}/dapr_${os}_${arch}.tar.gz" -o "$archive"
-          tar -xzf "$archive" -C "$install_dir" dapr
-          chmod +x "${install_dir}/dapr"
-          echo "$install_dir" >> "$GITHUB_PATH"
-          "${install_dir}/dapr" --version
-      - name: Run dapr init
-        uses: nick-fields/retry@ad984534de44a9489a53aefd81eb77f87c70dc60 # v4.0.0
+      - name: Install and initialize Dapr
+        uses: Hexalith/Hexalith.Builds/Github/dapr-init@main
         with:
-          timeout_minutes: 5
-          max_attempts: 3
-          retry_wait_seconds: 15
-          command: dapr init
+          version: ${{ inputs.dapr-version }}
diff --git a/.github/workflows/domain-release.yml b/.github/workflows/domain-release.yml
index 8b27bf8..0bc6437 100644
--- a/.github/workflows/domain-release.yml
+++ b/.github/workflows/domain-release.yml
@@ -87,37 +87,11 @@ jobs:
         run: dotnet restore ${{ inputs.solution }}
       - name: Build
         run: dotnet build ${{ inputs.solution }} --no-restore --configuration Release -warnaserror
-      - name: Install Dapr CLI
+      - name: Install and initialize Dapr
         if: ${{ inputs.test-projects != '' }}
-        shell: bash
-        env:
-          DAPR_VERSION: ${{ inputs.dapr-version }}
-        run: |
-          set -euo pipefail
-          version="${DAPR_VERSION#v}"
-          os="$(uname | tr '[:upper:]' '[:lower:]')"
-          case "$(uname -m)" in
-            x86_64) arch="amd64" ;;
-            aarch64) arch="arm64" ;;
-            armv7*) arch="arm" ;;
-            *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
-          esac
-          install_dir="${RUNNER_TEMP}/dapr-cli"
-          mkdir -p "$install_dir"
-          archive="${RUNNER_TEMP}/dapr-cli.tar.gz"
-          curl -fsSL "https://github.com/dapr/cli/releases/download/v${version}/dapr_${os}_${arch}.tar.gz" -o "$archive"
-          tar -xzf "$archive" -C "$install_dir" dapr
-          chmod +x "${install_dir}/dapr"
-          echo "$install_dir" >> "$GITHUB_PATH"
-          "${install_dir}/dapr" --version
-      - name: Run dapr init
-        if: ${{ inputs.test-projects != '' }}
-        uses: nick-fields/retry@ad984534de44a9489a53aefd81eb77f87c70dc60 # v4.0.0
+        uses: Hexalith/Hexalith.Builds/Github/dapr-init@main
         with:
-          timeout_minutes: 5
-          max_attempts: 3
-          retry_wait_seconds: 15
-          command: dapr init
+          version: ${{ inputs.dapr-version }}
diff --git a/Github/dapr-init/action.yml b/Github/dapr-init/action.yml
index 730598e..dbe20af 100644
--- a/Github/dapr-init/action.yml
+++ b/Github/dapr-init/action.yml
@@ -1,5 +1,5 @@
 name: 'Initialize Dapr'
-description: 'Installs the Dapr CLI and runs dapr init with retry'
+description: 'Installs the Dapr CLI and runs full dapr init with retry-safe cleanup'
@@ -11,32 +11,84 @@ runs:
   using: "composite"
   steps:
     - name: Install Dapr CLI
-      shell: bash
-      env:
-        DAPR_VERSION: ${{ inputs.version }}
-      run: |
-        set -euo pipefail
-        version="${DAPR_VERSION#v}"
-        os="$(uname | tr '[:upper:]' '[:lower:]')"
-        case "$(uname -m)" in
-          x86_64) arch="amd64" ;;
-          aarch64) arch="arm64" ;;
-          armv7*) arch="arm" ;;
-          *) echo "Unsupported architecture: $(uname -m)" >&2; exit 1 ;;
-        esac
-        install_dir="${RUNNER_TEMP}/dapr-cli"
-        mkdir -p "$install_dir"
-        archive="${RUNNER_TEMP}/dapr-cli.tar.gz"
-        curl -fsSL "https://github.com/dapr/cli/releases/download/v${version}/dapr_${os}_${arch}.tar.gz" -o "$archive"
-        tar -xzf "$archive" -C "$install_dir" dapr
-        chmod +x "${install_dir}/dapr"
-        echo "$install_dir" >> "$GITHUB_PATH"
-        "${install_dir}/dapr" --version
+      uses: dapr/setup-dapr@8d980918fd43a2765c143ce7f687665b2d46a6b9 # v2
+      with:
+        version: ${{ inputs.version }}
 
     - name: Initialize Dapr
       uses: nick-fields/retry@ad984534de44a9489a53aefd81eb77f87c70dc60 # v4.0.0
+      env:
+        DAPR_DEFAULT_IMAGE_REGISTRY: ghcr
+        DAPR_VERSION: ${{ inputs.version }}
+        GITHUB_TOKEN: ${{ github.token }}
       with:
         timeout_minutes: 5
         max_attempts: 3
         retry_wait_seconds: 15
-        command: dapr init
+        command: |
+          bash <<'BASH'
+          set -euo pipefail
+
+          version="${DAPR_VERSION#v}"
+
+          cleanup_dapr() {
+            echo "Cleaning any partial Dapr installation before init..."
+            dapr uninstall --all >/dev/null 2>&1 || true
+            rm -f \
+              "${HOME}/.dapr/bin/daprd" \
+              "${HOME}/.dapr/bin/placement" \
+              "${HOME}/.dapr/bin/scheduler"
+            if command -v docker >/dev/null 2>&1; then
+              docker rm -f dapr_placement dapr_scheduler dapr_redis dapr_zipkin >/dev/null 2>&1 || true
+            fi
+          }
+
+          port_is_free() {
+            python3 - "$1" <<'PY'
+          import socket
+          import sys
+
+          port = int(sys.argv[1])
+          with socket.socket(socket.AF_INET, socket.SOCK_STREAM) as listener:
+              listener.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
+              try:
+                  listener.bind(("127.0.0.1", port))
+              except OSError:
+                  sys.exit(1)
+          PY
+          }
+
+          wait_for_ports_free() {
+            local timeout_seconds="$1"
+            shift
+            local deadline=$((SECONDS + timeout_seconds))
+
+            while true; do
+              local busy_ports=()
+              local port
+              for port in "$@"; do
+                if ! port_is_free "$port"; then
+                  busy_ports+=("$port")
+                fi
+              done
+
+              if [ "${#busy_ports[@]}" -eq 0 ]; then
+                return 0
+              fi
+
+              if [ "$SECONDS" -ge "$deadline" ]; then
+                echo "Dapr init ports still busy after ${timeout_seconds}s: ${busy_ports[*]}" >&2
+                return 1
+              fi
+
+              echo "Waiting for Dapr init ports to become free: ${busy_ports[*]}"
+              sleep 2
+            done
+          }
+
+          cleanup_dapr
+          wait_for_ports_free 60 58080 58081 50005
+
+          dapr init --runtime-version "$version"
+          dapr --version
+          BASH
```
