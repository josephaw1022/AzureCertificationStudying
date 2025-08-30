# Azure Functions Core Tools (Linux CLI) Setup

These are the steps to install the **Azure Functions CLI (`func`)** manually on Linux without using a package manager.

---

## 1. Navigate to Home Directory

```bash
cd ~
````

---

## 2. Download the Latest Release

Visit the [Azure Functions Core Tools releases page](https://github.com/Azure/azure-functions-core-tools/releases)
and copy the link for the latest **Linux x64 ZIP**.

Example (replace with the latest version):

```bash
curl -LO https://github.com/Azure/azure-functions-core-tools/releases/download/4.0.5611/Azure.Functions.Cli.linux-x64.4.0.5611.zip
```

---

## 3. Extract the Release

Unzip the downloaded archive into a dedicated folder:

```bash
unzip -d azure-functions-cli Azure.Functions.Cli.linux-x64.*.zip
```

---

## 4. Cleanup the ZIP

Once extracted, you can remove the downloaded archive:

```bash
rm Azure.Functions.Cli.linux-x64.*.zip
```

---

## 5. Make Binaries Executable

```bash
cd azure-functions-cli
chmod +x func
chmod +x gozip
```

Test it with:

```bash
./func --version
```

---

## 6. Add `func` to Your PATH

Create a small shell script under `~/.bashrc.d/` to add the CLI folder to your `$PATH`:

```bash
echo 'export PATH="$HOME/azure-functions-cli:$PATH"' > ~/.bashrc.d/azure-functions-cli.sh
```

Make sure it’s executable:

```bash
chmod +x ~/.bashrc.d/azure-functions-cli.sh
```

Reload your shell:

```bash
source ~/.bashrc
```

---

## 7. Verify Installation

```bash
func --version
```