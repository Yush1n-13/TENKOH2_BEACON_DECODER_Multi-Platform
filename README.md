<p>
    <img src="Images/TK2.png" height="100px" />
    <img src="Images/TEN-KOH2.png" height="100px" />
</p>

# Ten-Koh2 CW Beacon Decoder

This is an application developed at the Okuyama Laboratory for decoding the CW beacon of the nanosatellite "Ten-Koh2". By inputting the CW beacon string excluding the callsign, you can grasp the status of the satellite. For details on the conversion method, please refer to the provided documentation.


## Features

- **Logging**: When the application is run, a folder named `logs` is created in the directory where the application is operating. The logs are saved in JSON format.
- **Decoding Modes**: The application offers two modes: NUM and JAM. Depending on the length of the input string, the tab where the decoded result is displayed will differ.
  - NUM Mode recognizes strings of 25 characters and displays results in the NUM Tab.
  - JAM Mode recognizes strings of 37 characters and displays results in the JAM Tab.
  - While decoding is done automatically, users may need to manually switch tabs to view the decoding results in certain situations.
- **Decoding Activation**: This function is automatically activated when the "DECODE" button is pressed. The timestamp is recorded at the moment the button is pressed.

## Automatic Input Mode

This feature allows automatic retrieval of the input string from the log file of an application called Fldigi. To utilize this mode, users need to select three options:

1. **Target String**: This represents the string to be targeted during the automatic read. A colon (` : `) will be appended automatically to its end. Due to occasional misinterpretation of the initial part of the callsign, this feature is made adjustable for users. If you notice discrepancies in the CWDECODE application, adjust the target string accordingly. By default, it's set to the satellite's callsign, "JS1YKI".

2. **Data Length to Read**: The satellite operates in two modes: `NominalMode` and `JamsatMissionMode`. Each mode has a different length, so please choose according to the active mode as it won't be detected automatically.

3. **Path to Read From**: There are two options here. If you choose a folder, make sure to select the folder containing the current date's log file. The application will then auto-detect the log file, which is named in the format `fldigiyyyymmdd.log`. Alternatively, you can directly select the file. This option is especially useful if you're operating Fldigi across different dates or want to specify a custom log file.

Settings are saved in `Settings.json` and are loaded automatically by the application. The file is generated in the application's directory, allowing users to manually edit it. If you edit and encounter non-functionality, please click on the "Restore" option in the settings tab to revert to default settings. The current configuration can be checked by pressing the settings button.

Note: If there are no updates for 2 minutes, or if the specified folder doesn't exist, the system will automatically revert to manual mode.

## Sample CW Beacon Strings

> 28fa37549eb67025ff19f19d0<br>
> 28fa37549eb6702fff19f19d0<br>
> 28fa37549eb67025ff01C5B37792270004000

## Compatibility

- Tested and confirmed to work on Windows x64 and Linux (WSL2-Ubuntu environment).

## Future Implementations

- OperationMode feature
- Information section
- Implementation of dark mode.
- Implementation of an error display area (only for path errors and session timeouts).
- More features to be considered.