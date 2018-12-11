﻿#region "copyright"

/*
    Copyright © 2016 - 2018 Stefan Berg <isbeorn86+NINA@googlemail.com>

    This file is part of N.I.N.A. - Nighttime Imaging 'N' Astronomy.

    N.I.N.A. is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    N.I.N.A. is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with N.I.N.A..  If not, see <http://www.gnu.org/licenses/>.
*/

#endregion "copyright"

using NINA.Model;
using NINA.Utility.Astrometry;
using NINA.Utility.Profile;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Threading;
using System.Threading.Tasks;

namespace NINA.Utility {

    internal class DatabaseInteraction {

        static DatabaseInteraction() {
            DllLoader.LoadDll("SQLite\\SQLite.Interop.dll");
        }

        private string _connectionString;

        public DatabaseInteraction(string dbLocation) {
            _connectionString = string.Format(@"Data Source={0};foreign keys=true;", dbLocation);
        }

        public async Task<ICollection<string>> GetConstellations(CancellationToken token) {
            const string query = "SELECT DISTINCT(constellation) FROM dsodetail;";
            var constellations = new List<string>() { string.Empty };

            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            constellations.Add(reader["constellation"].ToString());
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);
            }

            return constellations;
        }

        public async Task<ICollection<string>> GetObjectTypes(CancellationToken token) {
            const string query = "SELECT DISTINCT(dsotype) FROM dsodetail;";
            var dsotypes = new List<string>() { };
            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            dsotypes.Add(reader["dsotype"].ToString());
                        }
                    }
                }
            } catch (Exception ex) {
                Logger.Error(ex);
                Notification.Notification.ShowError(ex.Message);
            }

            return dsotypes;
        }

        public async Task<List<DeepSkyObject>> GetDeepSkyObjects(
            string imageRepository,
            CancellationToken token,
            string constellation = "",
            double? rafrom = null,
            double? rathru = null,
            double? decfrom = null,
            double? decthru = null,
            string sizefrom = null,
            string sizethru = null,
            IList<string> dsotypes = null,
            string brightnessfrom = null,
            string brightnessthru = null,
            string magnitudefrom = null,
            string magnitudethru = null,
            string searchobjectname = null,
            string orderby = "id",
            string orderbydirection = "ASC") {
            string query = @"SELECT id, ra, dec, dsotype, magnitude, sizemax, group_concat(cataloguenr.catalogue || ' ' || cataloguenr.designation) aka, constellation, surfacebrightness
                             FROM dsodetail
                                INNER JOIN cataloguenr on dsodetail.id = cataloguenr.dsodetailid
                             WHERE (1=1) ";

            if (constellation != null && constellation != string.Empty) {
                query += " AND constellation = $constellation ";
            }

            if (rafrom != null) {
                query += " AND ra >= $rafrom ";
            }

            if (rathru != null) {
                query += " AND ra <= $rathru ";
            }

            if (decfrom != null) {
                query += " AND dec >= $decfrom ";
            }

            if (decthru != null) {
                query += " AND dec <= $decthru ";
            }

            if (sizefrom != null && sizefrom != string.Empty) {
                query += " AND sizemin >= $sizefrom ";
            }

            if (sizethru != null && sizethru != string.Empty) {
                query += " AND sizemax <= $sizethru ";
            }

            if (dsotypes != null && dsotypes.Count > 0) {
                query += " AND dsotype IN (";
                for (int i = 0; i < dsotypes.Count; i++) {
                    query += "$dsotype" + i.ToString() + ",";
                }
                query = query.Remove(query.Length - 1);
                query += ") ";
            }

            if (brightnessfrom != null && brightnessfrom != string.Empty) {
                query += " AND surfacebrightness >= $brightnessfrom ";
            }

            if (brightnessthru != null && brightnessthru != string.Empty) {
                query += " AND surfacebrightness <= $brightnessthru ";
            }

            if (magnitudefrom != null && magnitudefrom != string.Empty) {
                query += " AND magnitude >= $magnitudefrom ";
            }

            if (magnitudethru != null && magnitudethru != string.Empty) {
                query += " AND magnitude <= $magnitudethru ";
            }

            query += " GROUP BY id ";

            if (searchobjectname != null && searchobjectname != string.Empty) {
                searchobjectname = "%" + searchobjectname + "%";
                query += " HAVING aka LIKE $searchobjectname OR group_concat(cataloguenr.catalogue || cataloguenr.designation) LIKE $searchobjectname";
            }

            query += " ORDER BY " + orderby + " " + orderbydirection + ";";

            var dsos = new List<DeepSkyObject>();
            try {
                using (SQLiteConnection connection = new SQLiteConnection(_connectionString)) {
                    connection.Open();
                    using (SQLiteCommand command = connection.CreateCommand()) {
                        command.CommandText = query;

                        command.Parameters.AddWithValue("$constellation", constellation);
                        command.Parameters.AddWithValue("$rafrom", rafrom);
                        command.Parameters.AddWithValue("$rathru", rathru);
                        command.Parameters.AddWithValue("$decfrom", decfrom);
                        command.Parameters.AddWithValue("$decthru", decthru);
                        command.Parameters.AddWithValue("$sizefrom", sizefrom);
                        command.Parameters.AddWithValue("$sizethru", sizethru);
                        command.Parameters.AddWithValue("$brightnessfrom", brightnessfrom);
                        command.Parameters.AddWithValue("$brightnessthru", brightnessthru);
                        command.Parameters.AddWithValue("$magnitudefrom", magnitudefrom);
                        command.Parameters.AddWithValue("$magnitudethru", magnitudethru);
                        command.Parameters.AddWithValue("$searchobjectname", searchobjectname);

                        if (dsotypes != null && dsotypes.Count > 0) {
                            for (int i = 0; i < dsotypes.Count; i++) {
                                command.Parameters.AddWithValue("$dsotype" + i.ToString(), dsotypes[i]);
                            }
                        }

                        var reader = await command.ExecuteReaderAsync(token);

                        while (reader.Read()) {
                            var dso = new DeepSkyObject(reader.GetString(0), imageRepository);

                            var coords = new Coordinates(reader.GetDouble(1), reader.GetDouble(2), Epoch.J2000, Coordinates.RAType.Degrees);
                            dso.Coordinates = coords;
                            dso.DSOType = reader.GetString(3);

                            if (!reader.IsDBNull(4)) {
                                dso.Magnitude = reader.GetDouble(4);
                            }

                            if (!reader.IsDBNull(5)) {
                                dso.Size = reader.GetDouble(5);
                            }

                            if (!reader.IsDBNull(6)) {
                                var akas = reader.GetString(6);
                                if (akas != string.Empty) {
                                    foreach (var name in akas.Split(',')) {
                                        dso.AlsoKnownAs.Add(name);
                                    }
                                }
                            }

                            if (!reader.IsDBNull(7)) {
                                dso.Constellation = reader.GetString(7);
                            }

                            if (!reader.IsDBNull(8)) {
                                dso.SurfaceBrightness = reader.GetDouble(8);
                            }

                            dsos.Add(dso);
                        }
                    }
                }
            } catch (Exception ex) {
                if (!ex.Message.Contains("Execution was aborted by the user")) {
                    Logger.Error(ex);
                    Notification.Notification.ShowError(ex.Message);
                }
            }

            return dsos;
        }
    }
}