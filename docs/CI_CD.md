# CI/CD & Build Pipeline Guide

This repository uses [GitHub Actions](https://github.com/features/actions) and [GameCI](https://game.ci/) to automate standalone player builds of the AUV simulator across Linux, Windows, and macOS.

---

## Table of Contents

- [Overview](#overview)
- [Unity Student License Setup](#unity-student-license-setup)
  - [Activating Unity Student](#activating-unity-student)
  - [Finding Your Serial Number](#finding-your-serial-number)
  - [Subscription Expiration Notice](#subscription-expiration-notice)
- [GitHub Secrets Configuration](#github-secrets-configuration)
- [Pipeline Architecture](#pipeline-architecture)
  - [Concurrency & License Seat Management](#concurrency--license-seat-management)
  - [Library Caching](#library-caching)
- [Running Builds](#running-builds)
  - [Manual Trigger (workflow_dispatch)](#manual-trigger-workflow_dispatch)
  - [Release Tag Trigger](#release-tag-trigger)
  - [Downloading & Running Build Artifacts](#downloading--running-build-artifacts)
- [Future Expansion: Adding Unit Tests (unity-test-runner)](#future-expansion-adding-unit-tests-unity-test-runner)
  - [Build vs. Test Runner](#build-vs-test-runner)
  - [How to Set Up Tests](#how-to-set-up-tests)
  - [Sample Test Workflow](#sample-test-workflow)

---

## Overview

The build workflow is defined in [`.github/workflows/build.yml`](../.github/workflows/build.yml). It supports:
- **StandaloneLinux64**: Linux x86_64 standalone executable (`auv-sim.x86_64` for Ubuntu / ROS 2 environments).
- **StandaloneWindows64**: Windows 64-bit standalone executable (`auv-sim.exe`).
- **StandaloneOSX**: macOS Universal / Intel / Apple Silicon application bundle (`auv-sim.app`).

Because `mcgill-robotics/auv-sim-unity` is a **public repository**, GitHub Actions provides free and unlimited standard runner minutes.

---

## Unity Student License Setup

Unity requires a license to run in headless / batchmode inside CI Docker containers. We use a **Unity Student serial license**, which allows automated command-line activation.

### Activating Unity Student

1. Sign up for the GitHub Student Developer Pack or verify student status through Unity's education verification portal (SheerID).
2. Activate your **Unity Student Plan** on your Unity ID account.

### Finding Your Serial Number

Once your student plan is activated:
1. Navigate to [https://id.unity.com/](https://id.unity.com/) and log in.
2. Go to **Subscriptions** (or **Seats** / **My Account**).
3. Find your active **Unity Student** plan.
4. Copy the **Serial Number** (formatted as `SC-XXXX-XXXX-XXXX-XXXX-XXXX` or `U3-XXXX-XXXX-XXXX-XXXX-XXXX`).

### Subscription Expiration Notice

> [!IMPORTANT]
> **Active Subscription Expiration:** **October 3, 2027**  
> Notice in Unity ID: *"Your subscription will expire on October 3, 2027."*  
> When this date arrives, CI builds will fail during the `unity-builder` activation step. A team member must re-verify student status at [https://id.unity.com/](https://id.unity.com/) and update the `UNITY_SERIAL` repository secret with the renewed key.

---

## GitHub Secrets Configuration

To enable automated builds, the following repository secrets must be configured in GitHub:

1. Navigate to the repository on GitHub: `mcgill-robotics/auv-sim-unity`.
2. Go to **Settings** > **Secrets and variables** > **Actions**.
3. Under **Repository secrets**, click **New repository secret** and add:

| Secret Name | Description | Example |
| :--- | :--- | :--- |
| `UNITY_SERIAL` | Unity Student serial number from [id.unity.com](https://id.unity.com/) | `SC-XXXX-XXXX-XXXX-XXXX-XXXX` |
| `UNITY_EMAIL` | Email associated with the Unity ID account holding the license | `member@mcgillrobotics.com` |
| `UNITY_PASSWORD` | Password for the Unity ID account | `••••••••••••` |

> [!WARNING]
> **Two-Factor Authentication (2FA):**  
> Unity's CLI batchmode activation cannot handle interactive 2FA challenges. If the Unity ID account has 2FA enabled, CLI logins will be rejected. Use a dedicated machine/CI Unity account without 2FA or with app-password authorization where the student seat is assigned.

---

## Pipeline Architecture

### Concurrency & License Seat Management

A Unity Student serial grants **one concurrent activation seat**. If two CI jobs attempt to use the same serial simultaneously, the second job fails with an activation error.

> [!NOTE]
> **No Command-Line License Return for Student / Personal Serials:**  
> The GameCI action `unity-return-license` is designed exclusively for **Unity Pro / Enterprise** floating licenses that use a shared license server. Unity Personal and Student licenses do not support `-returnlicense` via the CLI (attempting to do so triggers a dialog timeout). For Student plans, ephemeral CI Docker containers activate on demand and release automatically when the container terminates.

Seat conflicts are prevented via workflow-level serialization:
- **Workflow Concurrency Group:**
  ```yaml
  concurrency:
    group: unity-license
    cancel-in-progress: false
  ```
  Queues runs sequentially rather than executing them in parallel.
- **Matrix Serialization:** `max-parallel: 1` ensures multi-platform matrix jobs run one after the other without overlapping.

### Library Caching

The Unity `Library/` directory contains all processed assets, shader caches, and metadata (~3.5 GB raw). Reimporting these on every clean GitHub runner takes 15–25 minutes.

The workflow caches `Library/` using `actions/cache@v6`:
```yaml
- name: Restore Library Cache
  uses: actions/cache@v6
  with:
    path: Library
    key: Library-Build-${{ matrix.targetPlatform }}-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
    restore-keys: |
      Library-Build-${{ matrix.targetPlatform }}-
      Library-
```
Compressed cache size is ~1.4 GB, comfortably under GitHub's 10 GB repository cache quota. Subsequent builds only recompile modified assets and take 3–5 minutes.

---

## Running Builds

### Manual Trigger (workflow_dispatch)

1. Open the repository on GitHub and go to the **Actions** tab.
2. In the left sidebar, click **Build Simulator**.
3. Click the **Run workflow** dropdown on the right:
   - Select the branch (e.g. `main`).
   - Select the **Target platform to build**:
     - `StandaloneLinux64` (default)
     - `StandaloneWindows64`
     - `StandaloneOSX`
     - `All` (builds Linux, Windows, and Mac sequentially)
4. Click the green **Run workflow** button.

### Release Tag Trigger & Automated Draft Releases

Pushing any git tag matching `v*` automatically triggers builds for Linux, Windows, and macOS:

```bash
git tag v1.0.0
git push origin v1.0.0
```

When all platform builds finish:
1. A **`release`** job automatically downloads the artifacts from each build job.
2. It packages them into `.zip` archives (`auv-sim-StandaloneLinux64.zip`, `auv-sim-StandaloneWindows64.zip`, `auv-sim-StandaloneOSX.zip`).
3. It creates a **Draft GitHub Release** with auto-generated release notes (listing PRs and commits since the last release) and attaches all platform archives directly to the release.
4. Developers can review the draft under the repository's [Releases](https://github.com/mcgill-robotics/auv-sim-unity/releases) page and click **"Publish release"**.

### Downloading & Running Build Artifacts

There are two ways to obtain builds:

#### 1. From GitHub Releases (Recommended for Tags / Milestones)
Go to the repository's **Releases** tab (`mcgill-robotics/auv-sim-unity/releases`) to download the attached platform zip directly without needing to navigate Actions runs.

#### 2. From GitHub Actions Run Summary (For Manual Workflow Runs)
1. In GitHub, go to **Actions** -> **Build Simulator**.
2. Click on the completed workflow run.
3. Scroll down to the **Artifacts** section at the bottom of the Summary page.
4. Click on the desired artifact (`auv-sim-StandaloneLinux64`, `auv-sim-StandaloneWindows64`, or `auv-sim-StandaloneOSX`) to download the zip file.

#### Running the Executable:
- **Linux:**
  ```bash
  unzip auv-sim-StandaloneLinux64.zip -d auv-sim
  cd auv-sim
  chmod +x auv-sim.x86_64
  ./auv-sim.x86_64
  ```
- **macOS:**
  ```bash
  unzip auv-sim-StandaloneOSX.zip -d auv-sim
  cd auv-sim
  # Remove quarantine flag if macOS blocks opening
  xattr -cr auv-sim.app
  open auv-sim.app
  ```
- **Windows:** Extract `auv-sim-StandaloneWindows64.zip` and run `auv-sim.exe`.

---

## Future Expansion: Adding Unit Tests (unity-test-runner)

### Build vs. Test Runner

| Action | What it Does | When to Use |
| :--- | :--- | :--- |
| **`game-ci/unity-builder`** | Compiles project scenes into a runnable standalone player (`.x86_64` / `.exe` / `.app`). | For releases, testing deployment binaries, running headless sim on robot/server. |
| **`game-ci/unity-test-runner`** | Opens Unity Editor in batchmode and executes NUnit tests without building an executable. | For pull request checks to prevent regressions in physics, sensor math, or ROS message formatting. |

Automated testing is currently omitted to keep the pipeline lightweight since the project does not yet include NUnit test suites.

### How to Set Up Tests

If anyone on the team wishes to add automated unit tests:

1. **Create Test Folders & Assembly Definitions:**
   - Under `Assets/_Project/Tests/Editor/`:
     - Create an assembly definition `AuvSim.Tests.Editor.asmdef` with platforms set to `Editor` and references to `UnityEditor.TestRunner` and `UnityEngine.TestRunner`.
   - Under `Assets/_Project/Tests/PlayMode/`:
     - Create an assembly definition `AuvSim.Tests.PlayMode.asmdef` with references to `UnityEngine.TestRunner`.

2. **Write NUnit Tests:**
   ```csharp
   using NUnit.Framework;

   public class SensorMathTests
   {
       [Test]
       public void DvlVelocity_CalculatesCorrectMagnitude()
       {
           Assert.AreEqual(1.0f, 1.0f);
       }
   }
   ```

### Sample Test Workflow

Create `.github/workflows/test.yml`:

```yaml
name: Unity Tests

on:
  pull_request:
    branches:
      - main
  workflow_dispatch:

concurrency:
  group: unity-license
  cancel-in-progress: false

jobs:
  test:
    name: Run Tests
    runs-on: ubuntu-latest
    steps:
      - name: Checkout Repository
        uses: actions/checkout@v7
        with:
          lfs: true

      - name: Restore Library Cache
        uses: actions/cache@v6
        with:
          path: Library
          key: Library-Test-${{ hashFiles('Assets/**', 'Packages/**', 'ProjectSettings/**') }}
          restore-keys: |
            Library-Test-
            Library-

      - name: Run Unity Tests
        uses: game-ci/unity-test-runner@v4
        env:
          UNITY_EMAIL: ${{ secrets.UNITY_EMAIL }}
          UNITY_PASSWORD: ${{ secrets.UNITY_PASSWORD }}
          UNITY_SERIAL: ${{ secrets.UNITY_SERIAL }}
        with:
          unityVersion: 6000.0.62f1
          testMode: EditMode
          artifactsPath: artifacts/test-results
          githubToken: ${{ secrets.GITHUB_TOKEN }}
          checkName: 'Unity Test Results'
```
