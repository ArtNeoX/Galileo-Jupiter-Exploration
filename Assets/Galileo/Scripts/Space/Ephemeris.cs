using System;
using UnityEngine;

namespace Galileo.Space
{
    public enum Planet { Venus, Earth, Jupiter }

    /// <summary>
    /// Heliocentric planet positions from JPL's "Keplerian Elements for Approximate Positions of the
    /// Major Planets" (Standish, table 1, valid 1800–2050). Accurate to well under a degree in 1989–1995:
    /// it reproduces Jupiter's opposition of 27 Dec 1989 and Venus's inferior conjunction of 18 Jan 1990.
    /// Returns ecliptic x/y in AU (x towards the vernal equinox), i.e. the view from above the Solar System.
    /// </summary>
    public static class Ephemeris
    {
        //                     a            a/cy        e           e/cy         I            I/cy         L              L/cy             long.peri     /cy          node          /cy
        static readonly double[] VenusEl   = { 0.72333566, 0.00000390, 0.00677672, -0.00004107, 3.39467605, -0.00078890, 181.97909950, 58517.81538729, 131.60246718, 0.00268329, 76.67984255, -0.27769418 };
        static readonly double[] EarthEl   = { 1.00000261, 0.00000562, 0.01671123, -0.00004392, -0.00001531, -0.01294668, 100.46457166, 35999.37244981, 102.93768193, 0.32327364, 0.0, 0.0 };
        static readonly double[] JupiterEl = { 5.20288700, -0.00011607, 0.04838624, -0.00013253, 1.30439695, -0.00183714, 34.39644051, 3034.74612775, 14.72847983, 0.21252668, 100.47390909, 0.20469106 };

        static readonly DateTime J2000 = new DateTime(2000, 1, 1, 12, 0, 0, DateTimeKind.Utc);

        public static double OrbitalPeriodDays(Planet p)
        {
            var el = Elements(p);
            return 36525.0 * 360.0 / el[7];
        }

        public static Vector2 Position(Planet planet, DateTime date)
        {
            var el = Elements(planet);
            double T = (DateTime.SpecifyKind(date, DateTimeKind.Utc) - J2000).TotalDays / 36525.0;
            double a = el[0] + el[1] * T;
            double e = el[2] + el[3] * T;
            double I = el[4] + el[5] * T;
            double L = el[6] + el[7] * T;
            double peri = el[8] + el[9] * T;
            double node = el[10] + el[11] * T;

            double w = (peri - node) * Deg;
            double M = Wrap180(L - peri) * Deg;
            double E = M;
            for (int i = 0; i < 12; i++) E -= (E - e * Math.Sin(E) - M) / (1 - e * Math.Cos(E));

            double xp = a * (Math.Cos(E) - e);
            double yp = a * Math.Sqrt(1 - e * e) * Math.Sin(E);
            double O = node * Deg, inc = I * Deg;
            double cw = Math.Cos(w), sw = Math.Sin(w), cO = Math.Cos(O), sO = Math.Sin(O), ci = Math.Cos(inc);
            double x = (cw * cO - sw * sO * ci) * xp + (-sw * cO - cw * sO * ci) * yp;
            double y = (cw * sO + sw * cO * ci) * xp + (-sw * sO + cw * cO * ci) * yp;
            return new Vector2((float)x, (float)y);
        }

        const double Deg = Math.PI / 180.0;

        static double Wrap180(double d)
        {
            d %= 360.0;
            if (d > 180) d -= 360;
            if (d < -180) d += 360;
            return d;
        }

        static double[] Elements(Planet p) =>
            p == Planet.Venus ? VenusEl : p == Planet.Earth ? EarthEl : JupiterEl;
    }
}
