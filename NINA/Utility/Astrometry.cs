﻿using ASCOM.Astrometry;
using NINA.Utility.Profile;
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;

namespace NINA.Utility.Astrometry {

    public class Astrometry {
        private static ASCOM.Astrometry.AstroUtils.AstroUtils _astroUtils;

        public static ASCOM.Astrometry.AstroUtils.AstroUtils AstroUtils {
            get {
                if (_astroUtils == null) {
                    _astroUtils = new ASCOM.Astrometry.AstroUtils.AstroUtils();
                }
                return _astroUtils;
            }
        }

        private static ASCOM.Astrometry.NOVAS.NOVAS31 _nOVAS31;

        public static ASCOM.Astrometry.NOVAS.NOVAS31 NOVAS31 {
            get {
                if (_nOVAS31 == null) {
                    _nOVAS31 = new ASCOM.Astrometry.NOVAS.NOVAS31(); ;
                }
                return _nOVAS31;
            }
        }

        /// <summary>
        /// Convert degree to radians
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static double ToRadians(double val) {
            return (Math.PI / 180) * val;
        }

        /// <summary>
        /// Convert radians to degree
        /// </summary>
        /// <param name="angle"></param>
        /// <returns></returns>
        public static double ToDegree(double angle) {
            return angle * (180.0 / Math.PI);
        }

        public static double DegreeToArcmin(double degree) {
            return degree * 60;
        }

        public static double DegreeToArcsec(double degree) {
            return degree * 60 * 60;
        }

        public static double ArcminToArcsec(double arcmin) {
            return arcmin * 60;
        }

        public static double ArcminToDegree(double arcmin) {
            return arcmin / 60;
        }

        public static double ArcsecToArcmin(double arcsec) {
            return arcsec / 60;
        }

        public static double ArcsecToDegree(double arcsec) {
            return arcsec / 60 / 60;
        }

        public static double HoursToDegrees(double hours) {
            return hours * 15;
        }

        public static double DegreesToHours(double deg) {
            return deg / 15;
        }

        public static double GetLocalSiderealTimeNow(double longitude) {
            return GetLocalSiderealTime(DateTime.Now, longitude);
        }

        public static double GetJulianDate(DateTime date) {
            var utcdate = date.ToUniversalTime();
            return NOVAS31.JulianDate((short)utcdate.Year, (short)utcdate.Month, (short)utcdate.Day, utcdate.Hour + utcdate.Minute / 60.0 + utcdate.Second / 60.0 / 60.0);
        }

        /// <summary>
        /// </summary>
        /// <param name="date">     </param>
        /// <param name="longitude"></param>
        /// <returns>Sidereal Time in hours</returns>
        public static double GetLocalSiderealTime(DateTime date, double longitude) {
            var jd = GetJulianDate(date);

            /*
            var jd = AstroUtils.JulianDateUtc;
            var d = (jd - 2451545.0);
            var UT = DateTime.UtcNow.ToUniversalTime();
            var lst2 = 100.46 + 0.985647 * d + longitude + 15 * (UT.Hour + UT.Minute / 60.0 + UT.Second / 60.0 / 60.0);
            lst2 = (lst2 % 360) / 15;*/

            long jd_high = (long)jd;
            double jd_low = jd - jd_high;

            double lst = 0;
            NOVAS31.SiderealTime(jd_high, jd_low, NOVAS31.DeltaT(jd), ASCOM.Astrometry.GstType.GreenwichApparentSiderealTime, ASCOM.Astrometry.Method.EquinoxBased, ASCOM.Astrometry.Accuracy.Full, ref lst);
            lst = lst + DegreesToHours(longitude);
            return lst;
        }

        /// <summary>
        /// </summary>
        /// <param name="siderealTime">  </param>
        /// <param name="rightAscension"></param>
        /// <returns>Hour Angle in hours</returns>
        public static double GetHourAngle(double siderealTime, double rightAscension) {
            double hourAngle = siderealTime - rightAscension;
            if (hourAngle < 0) { hourAngle += 24; }
            return hourAngle;
        }

        /*
         * some handy formulas: http://www.stargazing.net/kepler/altaz.html
         */

        /// <summary>
        /// Calculates Altitude based on given input
        /// </summary>
        /// <param name="hourAngle">  in degrees</param>
        /// <param name="latitude">   in degrees</param>
        /// <param name="declination">in degrees</param>
        /// <returns></returns>
        public static double GetAltitude(double hourAngle, double latitude, double declination) {
            var radX = ToRadians(hourAngle);
            var sinAlt = Math.Sin(ToRadians(declination))
                         * Math.Sin(ToRadians(latitude))
                         + Math.Cos(ToRadians(declination))
                         * Math.Cos(ToRadians(latitude))
                         * Math.Cos(radX);
            var altitude = Astrometry.ToDegree(Math.Asin(sinAlt));
            return altitude;
        }

        /// <summary>
        /// Calculates Azimuth based on given input
        /// </summary>
        /// <param name="hourAngle">  in degrees</param>
        /// <param name="altitude">   in degrees</param>
        /// <param name="latitude">   in degrees</param>
        /// <param name="declination">in degrees</param>
        /// <returns></returns>
        public static double GetAzimuth(double hourAngle, double altitude, double latitude, double declination) {
            var radHA = ToRadians(hourAngle);
            var radAlt = ToRadians(altitude);
            var radLat = ToRadians(latitude);
            var radDec = ToRadians(declination);

            var cosA = (Math.Sin(radDec) - Math.Sin(radAlt) * Math.Sin(radLat)) /
                        (Math.Cos(radAlt) * Math.Cos(radLat));

            //fix double precision issues
            if (cosA < -1) { cosA = -1; }
            if (cosA > 1) { cosA = 1; }

            if (Math.Sin(radHA) < 0) {
                return ToDegree(Math.Acos(cosA));
            } else {
                return 360 - ToDegree(Math.Acos(cosA));
            }
        }

        public static RiseAndSetAstroEvent GetRiseAndSetEvent(DateTime date, EventType type, double latitude, double longitude) {
            var d = date.Day;
            var m = date.Month;
            var y = date.Year;

            /*The returned zero based arraylist has the following values:
             * Arraylist(0) - Boolean - True if the body is above the event limit at midnight (the beginning of the 24 hour day), false if it is below the event limit
             * Arraylist(1) - Integer - Number of rise events in this 24 hour period
             * Arraylist(2) - Integer - Number of set events in this 24 hour period
             * Arraylist(3) onwards - Double - Values of rise events in hours Arraylist
             * (3 + NumberOfRiseEvents) onwards - Double - Values of set events in hours*/
            var times = AstroUtils.EventTimes(type, d, m, y, latitude, longitude, TimeZone.CurrentTimeZone.GetUtcOffset(date).Hours + TimeZone.CurrentTimeZone.GetUtcOffset(date).Minutes / 60.0);

            if (times.Count > 3) {
                int nrOfRiseEvents = (int)times[1];
                int nrOfSetEvents = (int)times[2];

                double[] rises = new double[nrOfRiseEvents];
                double[] sets = new double[nrOfSetEvents];

                for (int i = 0; i < nrOfRiseEvents; i++) {
                    rises[i] = (double)times[i + 3];
                }

                for (int i = 0; i < nrOfSetEvents; i++) {
                    sets[i] = (double)times[i + 3 + nrOfRiseEvents];
                }

                if (rises.Count() > 0 && sets.Count() > 0) {
                    var rise = rises[0];
                    var set = sets[0];
                    return new RiseAndSetAstroEvent(date, rise, set);
                } else {
                    return null;
                }
            }
            return null;
        }

        public static RiseAndSetAstroEvent GetNightTimes(DateTime date, double latitude, double longitude) {
            return GetRiseAndSetEvent(date, EventType.AstronomicalTwilight, latitude, longitude);
        }

        public static RiseAndSetAstroEvent GetMoonRiseAndSet(DateTime date, double latitude, double longitude) {
            return GetRiseAndSetEvent(date, EventType.MoonRiseMoonSet, latitude, longitude);
        }

        public static RiseAndSetAstroEvent GetSunRiseAndSet(DateTime date, double latitude, double longitude) {
            return GetRiseAndSetEvent(date, EventType.SunRiseSunset, latitude, longitude);
        }

        /// <summary>
        /// Formats a given hours value into format "DD° MM' SS"
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static string DegreesToDMS(double value) {
            return DegreesToDMS(value, "{0:00}° {1:00}' {2:00}\"");
        }

        private static string DegreesToDMS(double value, string pattern) {
            bool negative = false;
            if (value < 0) {
                negative = true;
                value = -value;
            }
            if (negative) {
                pattern = "-" + pattern;
            }

            var degree = Math.Floor(value);
            var arcmin = Math.Floor(DegreeToArcmin(value - degree));
            var arcminDeg = ArcminToDegree(arcmin);

            var arcsec = Math.Round(DegreeToArcsec(value - degree - arcminDeg), 2);
            if (arcsec == 60) {
                /* If arcsec got rounded to 60 add to arcmin instead */
                arcsec = 0;
                arcmin += 1;

                if (arcmin == 60) {
                    /* If arcmin got rounded to 60 add to degree instead */
                    arcmin = 0;
                    degree += 1;
                }
            }

            return string.Format(pattern, degree, arcmin, arcsec);
        }

        /// <summary>
        /// Formats a given degree value into format "DD MM SS"
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static string DegreesToFitsDMS(double deg) {
            return DegreesToDMS(deg).Replace("°", "").Replace("'", "").Replace("\"", ""); ;
        }

        /// <summary>
        /// Formats a given degree value into format "DD:MM:SS"
        /// </summary>
        /// <param name="deg"></param>
        /// <returns></returns>
        public static string DegreesToHMS(double deg) {
            return DegreesToDMS(DegreesToHours(deg), "{0:00}:{1:00}:{2:00}");
        }

        /// <summary>
        /// Formats a given hours value into format "HH:MM:SS"
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string HoursToHMS(double hours) {
            return Utility.AscomUtil.HoursToHMS(hours);
        }

        /// <summary>
        /// Formats a given hours value into format "HH MM SS"
        /// </summary>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static string HoursToFitsHMS(double hours) {
            return Utility.AscomUtil.HoursToHMS(hours).Replace(':', ' ');
        }

        public static MoonPhase GetMoonPhase(DateTime date) {
            var phase = AstroUtils.MoonPhase(GetJulianDate(date));

            if ((phase >= -180.0 && phase < -135.0) || phase == 180.0) {
                return MoonPhase.FullMoon;
            } else if (phase >= -135.0 && phase < -90.0) {
                return MoonPhase.WaningGibbous;
            } else if (phase >= -90.0 && phase < -45.0) {
                return MoonPhase.LastQuarter;
            } else if (phase >= -45 && phase < 0.0) {
                return MoonPhase.WaningCrescent;
            } else if (phase >= 0.0 && phase < 45.0) {
                return MoonPhase.NewMoon;
            } else if (phase >= 45.0 && phase < 90.0) {
                return MoonPhase.WaxingCrescent;
            } else if (phase >= 90.0 && phase < 135.0) {
                return MoonPhase.FirstQuarter;
            } else if (phase >= 135.0 && phase < 180.0) {
                return MoonPhase.WaxingGibbous;
            } else {
                return MoonPhase.Unknown;
            }
        }

        public static double GetMoonIllumination(DateTime date) {
            return AstroUtils.MoonIllumination(Astrometry.GetJulianDate(date));
        }

        /// <summary>
        /// Calculates arcseconds per pixel for given pixelsize and focallength
        /// </summary>
        /// <param name="pixelSize">Pixel size in microns</param>
        /// <param name="focalLength">Focallength in mm</param>
        /// <returns></returns>
        public static double ArcsecPerPixel(double pixelSize, double focalLength) {
            // arcseconds inside one radian and compensated by the difference of microns in pixels and mm in focal length
            var factor = DegreeToArcsec(ToDegree(1)) / 1000d;
            return (pixelSize / focalLength) * factor;
        }

        public static double MaxFieldOfView(double arcsecPerPixel, double width, double height) {
            return Astrometry.ArcsecToArcmin(arcsecPerPixel * Math.Max(width, height));
        }

        public static double FieldOfView(double arcsecPerPixel, double width) {
            return Astrometry.ArcsecToArcmin(arcsecPerPixel * width);
        }

        public enum MoonPhase {
            Unknown,
            FullMoon,
            WaningGibbous,
            LastQuarter,
            WaningCrescent,
            NewMoon,
            WaxingCrescent,
            FirstQuarter,
            WaxingGibbous
        }

        public class RiseAndSetAstroEvent {

            public RiseAndSetAstroEvent(DateTime referenceDate, double rise, double set) {
                RiseDate = new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day, referenceDate.Hour, referenceDate.Minute, referenceDate.Second);
                if (RiseDate.Hour + RiseDate.Minute / 60.0 + RiseDate.Second / 60.0 / 60.0 > rise) {
                    RiseDate = RiseDate.AddDays(1);
                }
                RiseDate = RiseDate.Date;
                RiseDate = RiseDate.AddHours(rise);

                SetDate = new DateTime(referenceDate.Year, referenceDate.Month, referenceDate.Day, referenceDate.Hour, referenceDate.Minute, referenceDate.Second);
                if (SetDate.Hour + SetDate.Minute / 60.0 + SetDate.Second / 60.0 / 60.0 > set) {
                    SetDate = SetDate.AddDays(1);
                }
                SetDate = SetDate.Date;
                SetDate = SetDate.AddHours(set);
            }

            public DateTime RiseDate { get; private set; }
            public DateTime SetDate { get; private set; }
        }
    }

    [Serializable()]
    [XmlRoot(nameof(Coordinates))]
    public class Coordinates {

        private Coordinates() {
        }

        public enum RAType {
            Degrees,
            Hours
        }

        /// <summary>
        /// Right Ascension in hours
        /// </summary>
        [XmlElement(nameof(RA))]
        public double RA { get; set; }

        [XmlIgnore]
        public string RAString {
            get {
                return Astrometry.DegreesToHMS(RADegrees);
            }
        }

        /// <summary>
        /// Right Ascension in degrees
        /// </summary>
        public double RADegrees {
            get {
                return RA * 360 / 24;
            }
        }

        /// <summary>
        /// Declination in Degrees
        /// </summary>
        [XmlElement(nameof(Dec))]
        public double Dec { get; set; }

        [XmlIgnore]
        public string DecString {
            get {
                return Astrometry.DegreesToDMS(Dec);
            }
        }

        /// <summary>
        /// Epoch the coordinates are stored in. Either J2000 or JNOW
        /// </summary>
        [XmlElement(nameof(Epoch))]
        public Epoch Epoch { get; set; }

        /// <summary>
        /// Creates new coordinates
        /// </summary>
        /// <param name="ra">    Right Ascension in degrees or hours. RAType has to be set accordingly</param>
        /// <param name="dec">   Declination in degrees</param>
        /// <param name="epoch"> J2000|JNOW</param>
        /// <param name="ratype">Degrees|Hours</param>
        public Coordinates(double ra, double dec, Epoch epoch, RAType ratype) {
            this.RA = ra;
            this.Dec = dec;
            this.Epoch = epoch;

            if (ratype == RAType.Degrees) {
                this.RA = (this.RA * 24) / 360;
            }
        }

        /// <summary>
        /// Converts from one Epoch into another.
        /// </summary>
        /// <param name="targetEpoch"></param>
        /// <returns></returns>
        public Coordinates Transform(Epoch targetEpoch) {
            if (Epoch == targetEpoch) {
                return new Coordinates(this.RA, this.Dec, this.Epoch, RAType.Hours);
            }

            if (targetEpoch == Epoch.JNOW) {
                return TransformToJNOW();
            } else if (targetEpoch == Epoch.J2000) {
                return TransformToJ2000();
            } else {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Transforms coordinates from J2000 to JNOW
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJNOW() {
            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetJ2000(RA, Dec);
            return new Coordinates(transform.RAApparent, transform.DECApparent, Epoch.JNOW, RAType.Hours);
        }

        /// <summary>
        /// Transforms coordinates from JNOW to J2000
        /// </summary>
        /// <returns></returns>
        private Coordinates TransformToJ2000() {
            var transform = new ASCOM.Astrometry.Transform.Transform();
            transform.SetApparent(RA, Dec);
            return new Coordinates(transform.RAJ2000, transform.DecJ2000, Epoch.J2000, RAType.Hours);
        }

        /// <summary>
        /// Shift coordinates by a delta in degree
        /// </summary>
        /// <param name="deltaX">delta x in degree</param>
        /// <param name="deltaY">delta y in degree</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <returns></returns>
        public Coordinates Shift(double deltaX, double deltaY, double rotation) {
            var deltaXDeg = -deltaX;
            var deltaYDeg = -deltaY;
            var rotationRad = Astrometry.ToRadians(rotation);

            if (rotation != 0) {
                //Recalculate delta based on rotation
                //No spherical or other aberrations are assumed
                var originalDeltaX = deltaXDeg;
                deltaXDeg = deltaXDeg * Math.Cos(rotationRad) - deltaYDeg * Math.Sin(rotationRad);
                deltaYDeg = deltaYDeg * Math.Cos(rotationRad) + originalDeltaX * Math.Sin(rotationRad);
            }

            var originRARad = Astrometry.ToRadians(this.RADegrees);
            var originDecRad = Astrometry.ToRadians(this.Dec);

            var deltaXRad = Astrometry.ToRadians(deltaXDeg);
            var deltaYRad = Astrometry.ToRadians(deltaYDeg);

            // refer to http://faculty.wcas.northwestern.edu/nchapman/coding/worldpos.py

            var targetRARad = originRARad + Math.Atan2(deltaXRad, Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad));
            var targetDecRad =
                Math.Atan(
                    Math.Cos(targetRARad - originRARad)
                    * (deltaYRad * Math.Cos(originDecRad) + Math.Sin(originDecRad))
                    / (Math.Cos(originDecRad) - deltaYRad * Math.Sin(originDecRad))
                );

            var targetRA = Astrometry.ToDegree(targetRARad);
            if (targetRA < 0) { targetRA += 360; }
            if (targetRA >= 360) { targetRA -= 360; }

            var targetDec = Astrometry.ToDegree(targetDecRad);

            return new Coordinates(
                targetRA,
                targetDec,
                Epoch.J2000,
                Coordinates.RAType.Degrees
            );
        }

        /// <summary>
        /// Shift coordinates by a delta in pixel
        /// </summary>
        /// <param name="origin">Coordinates to shift from</param>
        /// <param name="deltaX">delta x</param>
        /// <param name="deltaY">delta y</param>
        /// <param name="rotation">rotation relative to delta values</param>
        /// <param name="scaleX">scale relative to deltaX in arcsecs</param>
        /// <param name="scaleY">scale raltive to deltaY in arcsecs</param>
        /// <returns></returns>
        public Coordinates Shift(
                double deltaX,
                double deltaY,
                double rotation,
                double scaleX,
                double scaleY
        ) {
            var deltaXDeg = deltaX * Astrometry.ArcsecToDegree(scaleX);
            var deltaYDeg = deltaY * Astrometry.ArcsecToDegree(scaleY);
            return this.Shift(deltaXDeg, deltaYDeg, rotation);
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Epoch {

        [Description("LblJ2000")]
        J2000,

        [Description("LblJNOW")]
        JNOW
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Hemisphere {

        [Description("LblNorthern")]
        NORTHERN,

        [Description("LblSouthern")]
        SOUTHERN
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum Direction {

        [Description("LblAltitude")]
        ALTITUDE,

        [Description("LblAzimuth")]
        AZIMUTH
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum AltitudeSite {

        [Description("LblEast")]
        EAST,

        [Description("LblWest")]
        WEST
    }
}