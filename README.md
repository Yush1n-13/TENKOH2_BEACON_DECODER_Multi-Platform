<p>    
    <img src="Images/TK2.png" height="100px" />
    <img src="Images/TEN-KOH2.png" height="100px" />
</p>

# Ten-Koh2 CW Beacon Decoder

This is an application developed at the Okuyama Laboratory for decoding the CW beacon of the nanosatellite "Ten-Koh2". By inputting the CW beacon string excluding the callsign, you can grasp the status of the satellite. For details on the conversion method, please refer to the provided documentation.


## Features

- **Logging**: When the application is run, a folder named `logs` is created in the directory where the application is operating. The logs are saved in JSON format.
- **Decoding Modes**: The application offers two modes: NUM and JAM. Depending on the length of the input string, the tab where the decoded result is displayed will differ. 
  - NUM Mode recognizes strings of 21 characters and displays results in the NUM Tab.
  - JAM Mode recognizes strings of 33 characters and displays results in the JAM Tab.
  - While decoding is done automatically, users may need to manually switch tabs to view the decoding results in certain situations.
- **Decoding Activation**: This function is automatically activated when the "DECODE" button is pressed. The timestamp is recorded at the moment the button is pressed.

## Sample CW Beacon Strings

> 28fa37549eb67025ff19f19d0  
> 28fa37549eb67025ff01C5B37792270004000   

## Compatibility

- Tested and confirmed to work on Windows x64 and Linux (WSL2-Ubuntu environment).

## Future Implementations

- OperationMode feature
- Information section