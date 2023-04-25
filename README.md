# LogCleaner

LogCleaner is a Dalamud plugin that automatically compresses your FFXIV logs using ZSTD and deletes them to help free up disk space.

## Installation

As of now, there is no precompiled binary available. You will need to compile the plugin yourself. Here's how to do it:

1. Clone the repository to your computer.
2. Navigate to the repository directory in your terminal.
3. Run the command `dotnet build -c Release`.
4. Copy the resulting `LogCleaner.dll` file to your `XIVLauncher\devPlugins` directory.

## Usage

Once installed, LogCleaner will automatically compress your FFXIV logs using ZSTD and delete them. To access the configuration window, type '/lc' in the chat window. From there, you can configure various settings, such as compression level and log retention period.

## Contributing

If you encounter any issues or have ideas for new features, please feel free to submit a pull request or open an issue on the GitHub repository.