# LocustInjector

A .NET tool that automatically injects the Locust ability into Warcraft III map files (.w3x/.w3m), making all units unable to be clicked.

## Features

- Batch process multiple maps at once
- Drag and drop support for individual maps
- Automatic backup with `_Locust` suffix
- Supports both .w3x and .w3m map formats
- Clean output organization with processed maps in `LocustMaps` folder

## Requirements

- .NET 10.0 Runtime or later
- Windows OS (for Warcraft III map files)

## Installation

1. Download the latest release from the [Releases](https://github.com/Chasinggoodgrades/LocustInjector/releases) page
2. Extract the contents to a folder of your choice
3. Run `LocustInjector.exe`

## Usage

### Method 1: Batch Processing

1. Run `LocustInjector.exe`
2. Place your .w3x or .w3m map files in the automatically created `Maps` folder
3. Run the executable again
4. Find your processed maps in the `LocustMaps` folder with the `_Locust` suffix

### Method 2: Drag and Drop

1. Drag a .w3x or .w3m file directly onto `LocustInjector.exe`
2. The tool will process the single file
3. Find the processed map in the `LocustMaps` folder

## How It Works

1. **Extraction**: Extracts the JASS script (war3map.j) from the MPQ archive
2. **Injection**: Adds trigger code that:
   - Creates initialization triggers
   - Adds & Removes the Locust ability to units entering the map for a half locust effect
   - Gives locust to new units entering the map
3. **Repackaging**: Injects the modified script back into a new map file

## Output Structure

```
   LocustInjector/
   LocustInjector.exe
   libs/                    # Dependencies
   locales/                 # Localization files
   Maps/                    # Input folder (your original maps)
   LocustMaps/              # Output folder (processed maps)
```

## Dependencies

This project uses the following War3Net libraries:
- War3Net.Build (6.0.1)
- War3Net.Build.Core (6.0.1)
- War3Net.CodeAnalysis.Decompilers (6.0.1)
- War3Net.IO.Compression (6.0.1)
- War3Net.IO.Mpq (6.0.1)

## Building from Source

```bash
# Clone the repository
git clone https://github.com/Chasinggoodgrades/LocustInjector.git
cd LocustInjector

# Build the project
dotnet build -c Release

# The executable will be in LocustInjector/bin/Release/net10.0/
```

## Troubleshooting

- **No maps found**: Make sure your map files are in the `Maps` folder and have .w3x or .w3m extensions
- **Processing failed**: Ensure the map file is not corrupted and is a valid Warcraft III map
- **Missing DLLs**: All dependencies should be in the `libs` folder after building

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is provided as-is for use with Warcraft III map editing.

## Acknowledgments

Built with [War3Net](https://github.com/Drake53/War3Net) - A collection of .NET libraries for Warcraft III modding.
