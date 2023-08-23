[![Health IT Logo](https://healthit.com.au/wp-content/uploads/2019/06/HIT-proper-logo.png)](https://healthit.com.au)

# TED (Tag Every Desktop) - a Health IT Project

TED is a commandline tool, inspired by the classic [BGInfo](https://learn.microsoft.com/en-us/sysinternals/downloads/bginfo), designed for MSPs to be able to display images and text programmatically on the desktop, positioned above the wallpaper but below the icons. It utilizes the bottom right corner of the primary monitor as the drawing area.

## Features

- Display images and text on the desktop
- Ability to specify different images based on perceived desktop luminance. Font color also adjusts between black or white based on perceived desktop luminance.
- Substitute system values in the text with special tokens
- DPi Aware
- Customizable with a variety of commandline switches
- Designed for deployment via an RMM

## Requirements

- Windows 8 or later

## Limitations

- Due to the nature of how this software draws, it will not work in a remote desktop environment due to rendering differences. You'll observe artifacts such as the image and/or text rendering then disappearing, smearing, or disappearing once moused over.
- As above, when the user changes desktop scaling, wallpaper, or resolution, the image will disappear.

One of the only ways I believe to get around these limitations would be to have TED run in the tray and redraw frequently and on Windows events (wallpaper changed, resized, etc.), but that's not something that's planned for the time being. PR's welcome!

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
- `-line`: The text to be drawn. This switch can be repeated multiple times to draw multiple lines of text. It can contain special tokens: `@userName`, `@machineName`, `@osName` and `@osVersion`. These tokens get substituted at runtime with system values for the operating system, current user, and machine name. If no lines are provided, it will render with the following by default:
  - "USERNAME: @userName"
  - "MACHINE NAME: @machineName"
  - "OS: @osName"

Example usage:

```shell
ted -di path/to/dark_image.png -li path/to/light_image.png -f Arial -fs 14 -ls 5 -hp 10 -vp 10 -line "Hello, @userName!" -line "You are using @osName on @machineName."
```

In terms of real world usage, we've found this to be a fantastic tool for helping clients quickly identify key information about their machine whilst on the phone with them.

## Examples

![TED Screenshot 1]( https://healthit.com.au/TEDScreenshot1_res1.png) ![TED Screenshot 2]( https://healthit.com.au/TEDScreenshot2_res1.png)

## Adding Tokens

Adding Tokens to the text system is simple, but will require editing the source and compiling your own binary.
Tokens are stored within TokenLookup inside Tokenizer.cs, found [here.](https://github.com/HealthITAU/TED/blob/main/src/TED/TED.Utils/Tokenizer.cs)

Simply add to this dictionary your token as the key and what you'd like to subtitute it with as the value.
Compile, and use your new tokens!

## Contributing

Contributions to TED are welcome! If you find any issues or have suggestions for improvement, please feel free to open an issue or submit a pull request.

## Supporting the project
:heart: the project and would like to show your support? Please consider donating to the following charities:
- [Black Dog](https://donate.blackdoginstitute.org.au/)
- [Cure4 CysticFibrosis Foundation](https://app.etapestry.com/onlineforms/Cure4CFFoundation/Donatenow.html)
- [Vinnies CEO Sleepout](https://www.ceosleepout.org.au/donation)
- [Care.org.au's Ukraine Humanitarian Crisis fund](https://www.care.org.au/appeals/ukraine-humanitarian-crisis/)
- [RedFrogs Australia](https://redfrogs.com.au/support/donate)
- [Love Your Sister (Sam's 1000)](https://www.loveyoursister.org/makeadonation)

## License

This project is licensed under the [GNU General Public License v3.0](https://github.com/HealthITAU/TED/blob/main/LICENSE)

## Contact

For any inquiries or further information, please contact the developers:
- [dev@healthit.com.au](mailto:dev@healthit.com.au?subject=[GitHub]%20TED%20Query)
