//
// http://pointofint.blogspot.fr/2014/06/sunrise-and-sunset-in-c.html
// No clear licence, seems to be by Peter Dotsenko
// Maybe original work comes from 
// http://www.codeproject.com/Articles/29306/C-Class-for-Calculating-Sunrise-and-Sunset-Times
// by Zacky Pickholz, licensed under public domain
//

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NAA
{
    public class Util
    {
        //*********************************************************************/

        // Convert radian angle to degrees

        static public double radToDeg(double angleRad)
        {
            return (180.0 * angleRad / Math.PI);
        }

        //*********************************************************************/

        // Convert degree angle to radians

        static public double degToRad(double angleDeg)
        {
            return (Math.PI * angleDeg / 180.0);
        }


        //***********************************************************************/
        //* Name: calcJD	
        //* Type: Function	
        //* Purpose: Julian day from calendar day	
        //* Arguments:	
        //* year : 4 digit year	
        //* month: January = 1	
        //* day : 1 - 31	
        //* Return value:	
        //* The Julian day corresponding to the date	
        //* Note:	
        //* Number is returned for start of day. Fractional days should be	
        //* added later.	
        //***********************************************************************/

        static public double calcJD(int year, int month, int day)
        {
            if (month <= 2)
            {
                year -= 1;
                month += 12;
            }
            double A = Math.Floor(year / 100.0);
            double B = 2 - A + Math.Floor(A / 4);

            double JD = Math.Floor(365.25 * (year + 4716)) + Math.Floor(30.6001 * (month + 1)) + day + B - 1524.5;
            return JD;
        }

        static public double calcJD(DateTime date)
        {
            return calcJD(date.Year, date.Month, date.Day);
        }

        //***********************************************************************/
        //* Name: calcTimeJulianCent	
        //* Type: Function	
        //* Purpose: convert Julian Day to centuries since J2000.0.	
        //* Arguments:	
        //* jd : the Julian Day to convert	
        //* Return value:	
        //* the T value corresponding to the Julian Day	
        //***********************************************************************/

        static public double calcTimeJulianCent(double jd)
        {
            double T = (jd - 2451545.0) / 36525.0;
            return T;
        }


        //***********************************************************************/
        //* Name: calcJDFromJulianCent	
        //* Type: Function	
        //* Purpose: convert centuries since J2000.0 to Julian Day.	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the Julian Day corresponding to the t value	
        //***********************************************************************/

        static public double calcJDFromJulianCent(double t)
        {
            double JD = t * 36525.0 + 2451545.0;
            return JD;
        }


        //***********************************************************************/
        //* Name: calGeomMeanLongSun	
        //* Type: Function	
        //* Purpose: calculate the Geometric Mean Longitude of the Sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the Geometric Mean Longitude of the Sun in degrees	
        //***********************************************************************/

        static public double calcGeomMeanLongSun(double t)
        {
            double L0 = 280.46646 + t * (36000.76983 + 0.0003032 * t);
            while (L0 > 360.0)
            {
                L0 -= 360.0;
            }
            while (L0 < 0.0)
            {
                L0 += 360.0;
            }
            return L0;	 // in degrees
        }


        //***********************************************************************/
        //* Name: calGeomAnomalySun	
        //* Type: Function	
        //* Purpose: calculate the Geometric Mean Anomaly of the Sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the Geometric Mean Anomaly of the Sun in degrees	
        //***********************************************************************/

        static public double calcGeomMeanAnomalySun(double t)
        {
            double M = 357.52911 + t * (35999.05029 - 0.0001537 * t);
            return M;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcEccentricityEarthOrbit	
        //* Type: Function	
        //* Purpose: calculate the eccentricity of earth's orbit	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* the unitless eccentricity	
        //***********************************************************************/


        static public double calcEccentricityEarthOrbit(double t)
        {
            double e = 0.016708634 - t * (0.000042037 + 0.0000001267 * t);
            return e;	 // unitless
        }

        //***********************************************************************/
        //* Name: calcSunEqOfCenter	
        //* Type: Function	
        //* Purpose: calculate the equation of center for the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* in degrees	
        //***********************************************************************/


        static public double calcSunEqOfCenter(double t)
        {
            double m = calcGeomMeanAnomalySun(t);

            double mrad = degToRad(m);
            double sinm = Math.Sin(mrad);
            double sin2m = Math.Sin(mrad + mrad);
            double sin3m = Math.Sin(mrad + mrad + mrad);

            double C = sinm * (1.914602 - t * (0.004817 + 0.000014 * t)) + sin2m * (0.019993 - 0.000101 * t) + sin3m * 0.000289;
            return C;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunTrueLong	
        //* Type: Function	
        //* Purpose: calculate the true longitude of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's true longitude in degrees	
        //***********************************************************************/


        static public double calcSunTrueLong(double t)
        {
            double l0 = calcGeomMeanLongSun(t);
            double c = calcSunEqOfCenter(t);

            double O = l0 + c;
            return O;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunTrueAnomaly	
        //* Type: Function	
        //* Purpose: calculate the true anamoly of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's true anamoly in degrees	
        //***********************************************************************/

        static public double calcSunTrueAnomaly(double t)
        {
            double m = calcGeomMeanAnomalySun(t);
            double c = calcSunEqOfCenter(t);

            double v = m + c;
            return v;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunRadVector	
        //* Type: Function	
        //* Purpose: calculate the distance to the sun in AU	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun radius vector in AUs	
        //***********************************************************************/

        static public double calcSunRadVector(double t)
        {
            double v = calcSunTrueAnomaly(t);
            double e = calcEccentricityEarthOrbit(t);

            double R = (1.000001018 * (1 - e * e)) / (1 + e * Math.Cos(degToRad(v)));
            return R;	 // in AUs
        }

        //***********************************************************************/
        //* Name: calcSunApparentLong	
        //* Type: Function	
        //* Purpose: calculate the apparent longitude of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's apparent longitude in degrees	
        //***********************************************************************/

        static public double calcSunApparentLong(double t)
        {
            double o = calcSunTrueLong(t);

            double omega = 125.04 - 1934.136 * t;
            double lambda = o - 0.00569 - 0.00478 * Math.Sin(degToRad(omega));
            return lambda;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcMeanObliquityOfEcliptic	
        //* Type: Function	
        //* Purpose: calculate the mean obliquity of the ecliptic	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* mean obliquity in degrees	
        //***********************************************************************/

        static public double calcMeanObliquityOfEcliptic(double t)
        {
            double seconds = 21.448 - t * (46.8150 + t * (0.00059 - t * (0.001813)));
            double e0 = 23.0 + (26.0 + (seconds / 60.0)) / 60.0;
            return e0;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcObliquityCorrection	
        //* Type: Function	
        //* Purpose: calculate the corrected obliquity of the ecliptic	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* corrected obliquity in degrees	
        //***********************************************************************/

        static public double calcObliquityCorrection(double t)
        {
            double e0 = calcMeanObliquityOfEcliptic(t);

            double omega = 125.04 - 1934.136 * t;
            double e = e0 + 0.00256 * Math.Cos(degToRad(omega));
            return e;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunRtAscension	
        //* Type: Function	
        //* Purpose: calculate the right ascension of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's right ascension in degrees	
        //***********************************************************************/

        static public double calcSunRtAscension(double t)
        {
            double e = calcObliquityCorrection(t);
            double lambda = calcSunApparentLong(t);

            double tananum = (Math.Cos(degToRad(e)) * Math.Sin(degToRad(lambda)));
            double tanadenom = (Math.Cos(degToRad(lambda)));
            double alpha = radToDeg(Math.Atan2(tananum, tanadenom));
            return alpha;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcSunDeclination	
        //* Type: Function	
        //* Purpose: calculate the declination of the sun	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* sun's declination in degrees	
        //***********************************************************************/

        static public double calcSunDeclination(double t)
        {
            double e = calcObliquityCorrection(t);
            double lambda = calcSunApparentLong(t);

            double sint = Math.Sin(degToRad(e)) * Math.Sin(degToRad(lambda));
            double theta = radToDeg(Math.Asin(sint));
            return theta;	 // in degrees
        }

        //***********************************************************************/
        //* Name: calcEquationOfTime	
        //* Type: Function	
        //* Purpose: calculate the difference between true solar time and mean	
        //*	 solar time	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* Return value:	
        //* equation of time in minutes of time	
        //***********************************************************************/

        static public double calcEquationOfTime(double t)
        {
            double epsilon = calcObliquityCorrection(t);
            double l0 = calcGeomMeanLongSun(t);
            double e = calcEccentricityEarthOrbit(t);
            double m = calcGeomMeanAnomalySun(t);

            double y = Math.Tan(degToRad(epsilon) / 2.0);
            y *= y;

            double sin2l0 = Math.Sin(2.0 * degToRad(l0));
            double sinm = Math.Sin(degToRad(m));
            double cos2l0 = Math.Cos(2.0 * degToRad(l0));
            double sin4l0 = Math.Sin(4.0 * degToRad(l0));
            double sin2m = Math.Sin(2.0 * degToRad(m));

            double Etime = y * sin2l0 - 2.0 * e * sinm + 4.0 * e * y * sinm * cos2l0
            - 0.5 * y * y * sin4l0 - 1.25 * e * e * sin2m;

            return radToDeg(Etime) * 4.0;	// in minutes of time
        }

        //***********************************************************************/
        //* Name: calcHourAngleSunrise	
        //* Type: Function	
        //* Purpose: calculate the hour angle of the sun at sunrise for the	
        //*	 latitude	
        //* Arguments:	
        //* lat : latitude of observer in degrees	
        //*	solarDec : declination angle of sun in degrees	
        //* Return value:	
        //* hour angle of sunrise in radians	
        //***********************************************************************/

        static public double calcHourAngleSunrise(double lat, double solarDec)
        {
            double latRad = degToRad(lat);
            double sdRad = degToRad(solarDec);

            double HAarg = (Math.Cos(degToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad));

            double HA = (Math.Acos(Math.Cos(degToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad)));

            return HA;	 // in radians
        }

        //***********************************************************************/
        //* Name: calcHourAngleSunset	
        //* Type: Function	
        //* Purpose: calculate the hour angle of the sun at sunset for the	
        //*	 latitude	
        //* Arguments:	
        //* lat : latitude of observer in degrees	
        //*	solarDec : declination angle of sun in degrees	
        //* Return value:	
        //* hour angle of sunset in radians	
        //***********************************************************************/

        static public double calcHourAngleSunset(double lat, double solarDec)
        {
            double latRad = degToRad(lat);
            double sdRad = degToRad(solarDec);

            double HAarg = (Math.Cos(degToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad));

            double HA = (Math.Acos(Math.Cos(degToRad(90.833)) / (Math.Cos(latRad) * Math.Cos(sdRad)) - Math.Tan(latRad) * Math.Tan(sdRad)));

            return -HA;	 // in radians
        }


        //***********************************************************************/
        //* Name: calcSunriseUTC	
        //* Type: Function	
        //* Purpose: calculate the Universal Coordinated Time (UTC) of sunrise	
        //*	 for the given day at the given location on earth	
        //* Arguments:	
        //* JD : julian day	
        //* latitude : latitude of observer in degrees	
        //* longitude : longitude of observer in degrees	
        //* Return value:	
        //* time in minutes from zero Z	
        //***********************************************************************/

        static public double calcSunriseUTC(double JD, double latitude, double longitude)
        {
            double t = calcTimeJulianCent(JD);

            // *** Find the time of solar noon at the location, and use
            // that declination. This is better than start of the 
            // Julian day

            double noonmin = calcSolNoonUTC(t, longitude);
            double tnoon = calcTimeJulianCent(JD + noonmin / 1440.0);

            // *** First pass to approximate sunrise (using solar noon)

            double eqTime = calcEquationOfTime(tnoon);
            double solarDec = calcSunDeclination(tnoon);
            double hourAngle = calcHourAngleSunrise(latitude, solarDec);

            double delta = longitude - radToDeg(hourAngle);
            double timeDiff = 4 * delta;	// in minutes of time
            double timeUTC = 720 + timeDiff - eqTime;	// in minutes

            // alert("eqTime = " + eqTime + "\nsolarDec = " + solarDec + "\ntimeUTC = " + timeUTC);

            // *** Second pass includes fractional jday in gamma calc

            double newt = calcTimeJulianCent(calcJDFromJulianCent(t) + timeUTC / 1440.0);
            eqTime = calcEquationOfTime(newt);
            solarDec = calcSunDeclination(newt);
            hourAngle = calcHourAngleSunrise(latitude, solarDec);
            delta = longitude - radToDeg(hourAngle);
            timeDiff = 4 * delta;
            timeUTC = 720 + timeDiff - eqTime; // in minutes

            // alert("eqTime = " + eqTime + "\nsolarDec = " + solarDec + "\ntimeUTC = " + timeUTC);

            return timeUTC;
        }

        //***********************************************************************/
        //* Name: calcSolNoonUTC	
        //* Type: Function	
        //* Purpose: calculate the Universal Coordinated Time (UTC) of solar	
        //*	 noon for the given day at the given location on earth	
        //* Arguments:	
        //* t : number of Julian centuries since J2000.0	
        //* longitude : longitude of observer in degrees	
        //* Return value:	
        //* time in minutes from zero Z	
        //***********************************************************************/

        static public double calcSolNoonUTC(double t, double longitude)
        {
            // First pass uses approximate solar noon to calculate eqtime
            double tnoon = calcTimeJulianCent(calcJDFromJulianCent(t) + longitude / 360.0);
            double eqTime = calcEquationOfTime(tnoon);
            double solNoonUTC = 720 + (longitude * 4) - eqTime; // min

            double newt = calcTimeJulianCent(calcJDFromJulianCent(t) - 0.5 + solNoonUTC / 1440.0);

            eqTime = calcEquationOfTime(newt);
            // double solarNoonDec = calcSunDeclination(newt);
            solNoonUTC = 720 + (longitude * 4) - eqTime; // min

            return solNoonUTC;
        }

        //***********************************************************************/
        //* Name: calcSunsetUTC	
        //* Type: Function	
        //* Purpose: calculate the Universal Coordinated Time (UTC) of sunset	
        //*	 for the given day at the given location on earth	
        //* Arguments:	
        //* JD : julian day	
        //* latitude : latitude of observer in degrees	
        //* longitude : longitude of observer in degrees	
        //* Return value:	
        //* time in minutes from zero Z	
        //***********************************************************************/

        static public double calcSunSetUTC(double JD, double latitude, double longitude)
        {
            var t = calcTimeJulianCent(JD);
            var eqTime = calcEquationOfTime(t);
            var solarDec = calcSunDeclination(t);
            var hourAngle = calcHourAngleSunrise(latitude, solarDec);
            hourAngle = -hourAngle;
            var delta = longitude + radToDeg(hourAngle);
            var timeUTC = 720 - (4.0 * delta) - eqTime;	// in minutes
            return timeUTC;
        }

        static public double calcSunRiseUTC(double JD, double latitude, double longitude)
        {
            var t = calcTimeJulianCent(JD);
            var eqTime = calcEquationOfTime(t);
            var solarDec = calcSunDeclination(t);
            var hourAngle = calcHourAngleSunrise(latitude, solarDec);
            var delta = longitude + radToDeg(hourAngle);
            var timeUTC = 720 - (4.0 * delta) - eqTime;	// in minutes
            return timeUTC;
        }

        static public string getTimeString(double time, int timezone, double JD, bool dst)
        {
            var timeLocal = time + (timezone * 60.0);
            var riseT = calcTimeJulianCent(JD + time / 1440.0);
            timeLocal += ((dst) ? 60.0 : 0.0);
            return getTimeString(timeLocal);
        }

        static public DateTime? getDateTime(double time, int timezone, DateTime date, bool dst)
        {
            double JD = calcJD(date);
            var timeLocal = time + (timezone * 60.0);
            var riseT = calcTimeJulianCent(JD + time / 1440.0);
            timeLocal += ((dst) ? 60.0 : 0.0);
            return getDateTime(timeLocal, date);
        }

        static private string getTimeString(double minutes)
        {

            string output = "";

            if ((minutes >= 0) && (minutes < 1440))
            {
                var floatHour = minutes / 60.0;
                var hour = Math.Floor(floatHour);
                var floatMinute = 60.0 * (floatHour - Math.Floor(floatHour));
                var minute = Math.Floor(floatMinute);
                var floatSec = 60.0 * (floatMinute - Math.Floor(floatMinute));
                var second = Math.Floor(floatSec + 0.5);
                if (second > 59)
                {
                    second = 0;
                    minute += 1;
                }
                if ((second >= 30)) minute++;
                if (minute > 59)
                {
                    minute = 0;
                    hour += 1;
                }
                output = String.Format("{0} : {1}", hour, minute);
            }
            else
            {
                return "error";
            }

            return output;
        }

        static private DateTime? getDateTime(double minutes, DateTime date)
        {

            DateTime? retVal = null;

            if ((minutes >= 0) && (minutes < 1440))
            {
                var floatHour = minutes / 60.0;
                var hour = Math.Floor(floatHour);
                var floatMinute = 60.0 * (floatHour - Math.Floor(floatHour));
                var minute = Math.Floor(floatMinute);
                var floatSec = 60.0 * (floatMinute - Math.Floor(floatMinute));
                var second = Math.Floor(floatSec + 0.5);
                if (second > 59)
                {
                    second = 0;
                    minute += 1;
                }
                if ((second >= 30)) minute++;
                if (minute > 59)
                {
                    minute = 0;
                    hour += 1;
                }
                return new DateTime(date.Year, date.Month, date.Day, (int)hour, (int)minute, (int)second);
            }
            else
            {
                return retVal;
            }
        }
    }
}
