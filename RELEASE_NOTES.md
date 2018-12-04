# Version tbd

## Features

    ### Sequence
        - Enable/Disable sequence entries
    ### Plate Solving
        - Added interface for All Sky Platesolver
    ### Camera Control
        - Altair native driver support
        - ToupTek native driver support


## Bugfixes

    - Corrected Max Binning level for ASI Cameras
    - Focuser move command fixed where lots of move commands where sent by accident
    - Rotation value now considered for sequence import|export
    - Canon: Fixed bulb mode for exposure times <30s

## Improvements

    - Clear button for PHD2 Graph
    - Hide camera cooler controls when not available for current camera model
    - Zero Floating point numbers now displayed as "0.00" instead of ".00"
    - Show better exception message when an ASCOM Interop Exception occurs

___

# Version 1.7.0.0

## Features

    ### Framing Assistant
        - Mosaic planning for framing assistant
        - Added multiple new SkySurveys to choose from for framing
        - DSLR RAW files can now be loaded into framing assistant
    ### Sequences
        - Sequence multiple target planning. Import/Export also available 
        - Consider Rotation for framing assistant when set for a sequence during platesolves
    ### Filter Wheel
        - Added a manual FilterWheel for users without a motorized wheel or with a filter drawer. There will pop up a window and prompt the user when a filter change is requested    
    ### Rotators
        - Support for ASCOM Rotators added
        - A manual rotator option is added, for users without an automatic rotator. A pop up will show with the current angle and target angle for a user to manually rotate to
    ### Imaging
        - DSLR Users: Images will now always be saved as RAW to prevent any loss of data due to conversions
        - Aberration inspector added to imaging tab. It will show a 3x3 Panel containing the current image
        - Battery display for DSLRs
    ### Settings
        - Added ImageParameter $$RMSARCSEC$$ and $$FOCUSPOSITION$$
        - Latitude and Longitude can now be synced from application to telescope and vica versa (when supported).
        - Serial Relay (via USB) interaction for Nikon Bulb

## Bugfixes

    - Fixed Canon issue that a second exposure is accidentally triggered, causing the camera to be stuck
    - Fixed memory leak when using Free Image RAW Converter

## Improvements

    - Guider is also connected when pushing "Connect All"
    - Changed FilterWheel UI for switching filters
    - Some improvements to memory consumption
    - Automatically triggered Autofocus and Platesolving now spawns inside a separate window
    - Guiding Dither thresholds now configurable
    - Flipped + and - for Stepper Controls
    - Minor UI Layout improvements
    - Made SkySurvey Cache directory configurable
    - Local astrometry.net client will now downscale image for faster image solving
    - ImagePatterns inside options can now be dragged from the list to the textbox
    - Major code refactorings for better maintainability
    - Lots and lots of minor bugfixes and improvements