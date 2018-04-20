# N.I.N.A. - Nighttime Imaging 'N' Astronomy #


**System Requirements**

* 64-bit Windows 7 or later
* (32-bit version is available, but may be unstable due to memory consumption)

* .NET Framework 4.6.2 or later

* ASCOM Platform 6.3 or later


**Features**

* Camera Control
     * ASCOM Driver
          * Tested with ASI 1600 MMC, Atik-383L
     * Native ZWO Driver
          * Tested with ASI 1600 MMC
     * Nikon
          * Tested with Nikon D5100, D7100
          * Serial cable support for bulb exposures using either telescope snap port or serial cable using RTS signal
               * Bulb exposure with snap port tested with EQ6-R + D5100
     * Canon 
          * Tested with Canon EOS 550d, 60d
     * Temperature control 
          * Cool down routine for a set amount of time

* Telescope Control
     * ASCOM Driver
          * Tested with HEQ-5, EQ6-R

* Filterwheel Control
     * ASCOM Driver
          * Tested with Starlight XPress motorized Filterwheel, Atik EFW2

* Autofocuser Control
     * ASCOM Driver
          * Tested with Lacerta Motorfocus

* Autoguider Control
     * via communication with PHD2 Server

* Advanced Sequencing
     * Import/Export sequences for planning ahead
     * Dithering (via PHD2)
     * Dithering after a set amount of images
     * Macros to set up custom file names
     * Supported Image formats: FITS, TIFF, XISF
     * Automatic Meridian Flip

* Autofocus
     * Triggered manually
     * During sequences
          * On start
          * After filter change

* Image Recognition
     * Statistics
     * Auto-Stretch
     * Star detection and HFR calculation, including stats history during imaging session

* Platesolving
     * Astrometry.net
     * Local instance of Astrometry.net and cygwin
     * Platesolve2 by Planewave

* Polar alignment assistant 
     * Polaris position in polar scope 
     * Precise PA Error calculation using platesolving 
     * DARV alignment procedure

* Sky Atlas
     * Detailed info for over 10000 Deep Sky Objects
     * Advanced filtering to get just the Deep Sky Objects that are relevant for you
     * Calculated altitude chart for each object based on your position
     * Night time duration based on your position
     * Set object as sequence target

* Framing Assistant
     * Multiple ways of importing an image for framing
          * Digital Sky Survey (requires internet connection)
          * Image File (tif, png, jpg)
          * Image Cache (from a previously loaded image)
     * By entering camera and telescope specs a rectangle with the respective field of view is generated
     * The rectangle can be rotated and dragged to the desired location
     * Once satisfied the coordinates where the rectangle is located can be set for a sequence to start imaging

* Image History
     * Thumbnail and statistics of images during an image session
     * Reload images of one session to the UI

* Fully customizable UI colors together with a bunch of preset Themes

* Weather data by OpenWeatherMap 


** Feedback**

Through the issue tracker

a mail to: isbeorn86+NINA@googlemail.com

or directly via Discord: http://discord.gg/fwpmHU4

** Credit **

Some icons made by 
[Madebyoliver](http://www.flaticon.com/authors/madebyoliver),
[Bogdan Rosu](http://www.flaticon.com/authors/bogdan-rosu),
[Appzgear](http://www.flaticon.com/authors/appzgear),
[Dale Humphries](http://www.flaticon.com/authors/dale-humphries) and
[Dave Gandy](http://www.flaticon.com/authors/dave-gandy)
from
[Flaticon](http://www.flaticon.com)
licenced by 
[Creative Commons BY 3.0](http://creativecommons.org/licenses/by/3.0/)

DCRaw (for Canon RAW image processing) - https://www.cybercom.net/~dcoffin/dcraw/