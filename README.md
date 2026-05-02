[![Health IT Logo](https://healthit.com.au/wp-content/uploads/2019/06/HIT-proper-logo.png)](https://healthit.com.au)

# TED (Tag Every Desktop) - a Health IT Project

TED is a command-line tool, inspired by the classic [BGInfo](https://learn.microsoft.com/en-us/sysinternals/downloads/bginfo), designed for MSPs to be able to display images and text programmatically on the desktop, positioned above the wallpaper but below the icons. It utilizes the bottom right corner of the primary monitor as the drawing area.

TED runs as a lightweight desktop process so it can repaint itself when Windows redraws the desktop. This avoids modifying or replacing the user's wallpaper.

## Features

- Display images and text on the desktop
- Ability to specify different images based on perceived desktop luminance. Font color also adjusts between black or white based on perceived desktop luminance.
- Substitute system values in the text with special tokens
- DPI aware
- Persistent desktop overlay that redraws itself without replacing the user's wallpaper
- Customizable with a variety of command-line switches
- Designed for deployment via an RMM

## Requirements

- Windows 8 or later

## Limitations

In remote desktop environments, Explorer and GPU composition behavior can still vary between Windows versions and client settings.

## Installation

Download the latest compiled binary for TED. You can find the latest downloads for TED below - this ensures your RMM always grabs the latest version!
- [x64](https://github.com/HealthITAU/TED/releases/latest/download/TED-x64.exe)
- [x86](https://github.com/HealthITAU/TED/releases/latest/download/TED-x86.exe)
- [winarm64](https://github.com/HealthITAU/TED/releases/latest/download/TED-winarm64.exe)

We recommend managing and deploying TED via your RMM. 

## Usage

TED supports the following switches:

- `-i` or `-image`: Path or URL to the image to be drawn.
- `-di` or `-darkimage`: Path or URL to the image to be drawn when the perceived desktop luminance is light.
- `-li` or `-lightimage`: Path or URL to the image to be drawn when the perceived desktop luminance is dark.
- `-f` or `-font`: Name of the font to use. Default is **Arial**.
- `-fs` or `-fontsize`: Font size in pixels. Default is **8**.
- `-ls` or `-linespacing`: Space between text lines in pixels. Default is **8**.
- `-hp` or `-hpad`: Horizontal padding amount in pixels. Default is **10**.
- `-vp` or `-vpad`: Vertical padding amount in pixels. Default is **10**.
- `-w` or `-width`: The width of the image when drawn, in pixels. By default this is **-1**. 
  - A value of -1 disables fixed width scaling and instead uses automatic image scaling to resize (respecting aspect ratio) the image to the size of the longest line of text.
- `-a` or `-align`: How the text should be aligned. Default is **Left**. Accepted values are **Left**, **Center** or **Right**. Not case-sensitive.
- `-line`: The text to be drawn. This switch can be repeated multiple times to draw multiple lines of text. Lines can contain system tokens and inline rich text formatting, both documented below. If no lines are provided, TED renders the following by default:
  - "USERNAME: @userName"
  - "MACHINE NAME: @machineName"
  - "OS: @osName"

### Line tokens

Tokens can be used inside any `-line` value. TED substitutes them at runtime with values from the current Windows session, machine identity, operating system, and primary network connection.

| Token | Runtime value |
| --- | --- |
| `@userName` | Current Windows user name |
| `@machineName` | Computer name |
| `@machineSerial` | Device serial number |
| `@manufacturer` | Device manufacturer |
| `@model` | Device model |
| `@ipAddress` | Primary IP address |
| `@macAddress` | Primary MAC address |
| `@osName` | Operating system name |
| `@osVersion` | Operating system version |

### Inline formatting

Lines also support a small set of inline rich text tags:

| Tag | Example |
| --- | --- |
| Bold | `<b>text</b>` |
| Italic | `<i>text</i>` |
| Underline | `<u>text</u>` |
| Named color | `<color=green>text</color>` |
| Hex color | `<color=#800080>text</color>` |

Untagged text uses TED's luminance-based black or white text color. Tagged colors are drawn as specified.

## Examples

We've provided an example PowerShell script to make deploying with your RMM quick and easy. You can find the script [here.](https://github.com/HealthITAU/TED/blob/main/examples/rmm_deploy.ps1)

TED is a CLI tool and can be called like so:

```shell
ted -di path/to/dark_image.png -li path/to/light_image.png -f Arial -fs 14 -ls 5 -hp 10 -vp 10 -line "Hello, @userName!" -line "You are using @osName on @machineName."
```

Inline rich text formatting can be used inside lines:

```shell
ted -line "<color=purple>OS: </color><color=green>@osName</color>" -line "<b><u>Device:</u></b> <i>@machineName</i>"
```

In terms of real world usage, we've found this to be a fantastic tool for helping clients quickly identify key information about their machine whilst on the phone with them.

![TED Screenshot 1]( https://healthit.com.au/TEDScreenshot1_res1.png) ![TED Screenshot 2]( https://healthit.com.au/TEDScreenshot2_res1.png)

## Adding tokens

Adding tokens to the text system requires editing the source and compiling your own binary.
Tokens are stored in `TokenLookup` inside [`Tokenizer.cs`](https://github.com/HealthITAU/TED/blob/main/src/TED/TED.Utils/Tokenizer.cs).

Add your token as the dictionary key and the substituted value provider as the value, then compile and use your new token in a `-line` value.

## Contributing

Contributions to TED are welcome! If you find any issues or have suggestions for improvement, please feel free to open an issue or submit a pull request.

## Supporting the project

:heart: the project and would like to show your support? Please consider donating to one of our favourite charities:
- [Love Your Sister (Sam's 1000)](https://www.loveyoursister.org/makeadonation)
- [Black Dog](https://donate.blackdoginstitute.org.au/)
- [RedFrogs Australia](https://redfrogs.com.au/support/donate)

Please let us know if you have donated because of this project!

## License

This project is licensed under the [GNU General Public License v3.0](https://github.com/HealthITAU/TED/blob/main/LICENSE)

## Contact

For any inquiries or further information, please contact the developers:
- [dev@healthit.com.au](mailto:dev@healthit.com.au?subject=[GitHub]%20TED%20Query)
