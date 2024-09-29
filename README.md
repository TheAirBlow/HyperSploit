# HyperSploit
This is a simple zero depedencies utility to bypass HyperOS restrictions on bootloader unlocking.

## Why another tool?
1) This tool is much more user-friendly, is a single file and has zero dependencies
2) HyperSploit ships with an older version of Settings to rollback in case you have a patched version
3) This repo is licenced under an open-source licence instead of all rights reserved.

## Disclaimer
Unlocking the bootloader is your responsibility. \
By using this tool you acknowledge that:
1) Software might stop working properly
2) You can accidentally brick your device
3) Data that wasn't backed up will be lost
4) Warranty *may* be voided

## Requirements
Note: Each account can only unlock 1 phone per month and 3 phones per year.
1) Xiaomi must not have forced your account or device to go through qualification
2) A valid SIM-card must be inserted with access to the internet
3) You're running an official version of HyperOS

## Bypass
1) Open developer settings and open Mi Unlock Status
2) Request unlocking, it will for whatever reason log everything necessary to forge the binding request ourselves
   1. Xiaomi recently patched it out - they switched to RSA with the private key unknown.
   2. You can still rollback to an earlier Settings app though - and it works perfectly!
   3. The tool will prompt you to try and rollback if it detects a patched version.
3) We disable mobile internet and send a forged request
   1. ROM version is modified to be MIUI 14 instead of HyperOS.
   2. It might fail due to even more random and arbitrary restrictions.
4) Use the official [unlocking tool](https://en.miui.com/unlock/index.html) and check how much you have to wait
   1. Do not eject the SIM card as the phone will constantly contact Xiaomi's servers.
   2. Do not bind the account to another device or re-bind the same one.

## Error trying to downgrade the settings app
If you get `Failure [INSTALL_FAILED_USER_RESTRICTED: Install canceled by user]` at this stage, make sure to do the following in developer options:
1) Enable `Install via USB`
2) Enable `USB debugging (Security Settings)`
3) Near the end of the page, tap `Reset to default values` 5 times
4) After more options appear, disable `Turn on system optimization` (if it still fails, you may need to reboot)

## How to use
Note: If you're on MacOS or on Linux, install ADB and add it to `PATH`.
1) Download latest binary from [Releases](https://github.com/TheAirBlow/HyperSploit/releases) for your OS
2) Connect your Xiaomi device and run the executable

## FAQ
1) **Q:** Why does the unlock tool still remind me to wait for N hours? \
   **A:** This tool only bypasses HyperOS restrictions, you still have to comply with MIUI's.
2) **Q:** I see `Couldn't verify, wait a minute or two and try again` on my device. Why? \
   **A:** This is normal as we intentionally cut it off to forge a binding request ourselves.

## Licence
This project is licenced under [Mozilla Public License Version 2.0](https://github.com/TheAirBlow/HyperSploit/blob/main/LICENCE)

## Credits
- [MlgmXyysd](https://github.com/MlgmXyysd) for making [Xiaomi-HyperOS-BootLoader-Bypass](https://github.com/MlgmXyysd/Xiaomi-HyperOS-BootLoader-Bypass) on which this tool is largely based on
