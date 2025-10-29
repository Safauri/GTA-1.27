using MySql.Data.MySqlClient;
using System;
using System.Data;

#nullable disable

namespace GTAServer
{
    public class Database
    {
        private const string connectionString = "Server=localhost;Database=gta;Uid=username;Password=password;";

        private static MySqlConnection Create()
        {
            return new MySqlConnection(connectionString);
        }

        public static Int64 GetMemberCount(string platformName = null)
        {
            using (MySqlConnection connection = Create())
            {
                Int64 count = -1;

                try
                {
                    connection.Open();

                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        if (platformName != null)
                        {
                            command.CommandText = "SELECT COUNT(*) FROM members WHERE platform_name=@platform_name";
                            command.Parameters.AddWithValue("@platform_name", platformName);
                        }
                        else
                        {
                            command.CommandText = "SELECT COUNT(*) FROM members";
                        }
                        count = (Int64)command.ExecuteScalar();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [GetMemberCount] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

                return count;
            }
        }

        public static bool GetMemberByXuid(ref Globals.Member member, string xuid)
        {
            bool result = false;

            using (MySqlConnection connection = Create())
            {
                try
                {
                    connection.Open();

                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM members WHERE xuid=@xuid";
                        command.Parameters.AddWithValue("@xuid", xuid);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                member.id = reader.GetInt32("id");
                                member.xuid = reader.GetString("xuid");
                                member.gamertag = reader.GetString("gamertag");
                                member.crew_id = reader.GetString("crew_id");
                                member.crew_tag = reader.GetString("crew_tag");
                                member.expires = Convert.ToDateTime(reader["expires"]);
                                member.last_online = Convert.ToDateTime(reader["last_online"]);
                                member.session_ticket = reader.GetString("session_ticket");
                                member.session_key = reader.GetString("session_key");
                                member.linkdiscord = reader.GetInt32("linkdiscord");
                                member.discordcode = reader.GetString("discordcode");
                                member.discordid = reader.GetString("discordid");
                                member.gsinfo = reader.GetString("gsinfo");
                                member.gsjoin = reader.GetInt32("gsjoin");
                                member.gshost = reader.GetInt32("gshost");
                                member.banned = reader.GetInt32("banned");
                                member.platform_name = reader.GetString("platform_name");
                                result = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [GetMemberByXuid] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }

            return result;
        }

        public static bool GetMemberFromSessionTicket(ref Globals.Member member, string session_ticket)
        {
            bool result = false;

            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM members WHERE session_ticket=@session_ticket";
                        command.Parameters.AddWithValue("@session_ticket", session_ticket);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                member.id = reader.GetInt32("id");
                                member.xuid = reader.GetString("xuid");
                                member.gamertag = reader.GetString("gamertag");
                                member.crew_id = reader.GetString("crew_id");
                                member.crew_tag = reader.GetString("crew_tag");
                                member.expires = Convert.ToDateTime(reader["expires"]);
                                member.last_online = Convert.ToDateTime(reader["last_online"]);
                                member.session_ticket = reader.GetString("session_ticket");
                                member.session_key = reader.GetString("session_key");
                                member.linkdiscord = reader.GetInt32("linkdiscord");
                                member.discordcode = reader.GetString("discordcode");
                                member.discordid = reader.GetString("discordid");
                                member.gsinfo = reader.GetString("gsinfo");
                                member.gsjoin = reader.GetInt32("gsjoin");
                                member.gshost = reader.GetInt32("gshost");
                                member.banned = reader.GetInt32("banned");
                                member.platform_name = reader.GetString("platform_name");
                                result = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [GetMemberFromSessionTicket] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

                return result;
            }
        }

        public static void AddMember(ref Globals.Member member)
        {
            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "INSERT INTO members (xuid, gamertag, crew_id, crew_tag, expires, last_online, session_ticket, session_key, platform_name) VALUES (@xuid, @gamertag, @crew_id, @crew_tag, @expires, @last_online, @session_ticket, @session_key, @platform_name)";
                        command.Parameters.AddWithValue("@xuid", member.xuid);
                        command.Parameters.AddWithValue("@gamertag", member.gamertag);
                        command.Parameters.AddWithValue("@crew_id", member.crew_id);
                        command.Parameters.AddWithValue("@crew_tag", member.crew_tag);
                        command.Parameters.AddWithValue("@expires", member.expires);
                        command.Parameters.AddWithValue("@last_online", member.last_online);
                        command.Parameters.AddWithValue("@session_ticket", member.session_ticket);
                        command.Parameters.AddWithValue("@session_key", member.session_key);
                        command.Parameters.AddWithValue("@platform_name", member.platform_name);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [AddMember] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public static void UpdateMember(ref Globals.Member member)
        {
            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE members SET last_online=@last_online, session_ticket=@session_ticket, session_key=@session_key, platform_name=@platform_name WHERE xuid=@xuid";
                        command.Parameters.AddWithValue("@xuid", member.xuid);
                        command.Parameters.AddWithValue("@last_online", member.last_online);
                        command.Parameters.AddWithValue("@session_ticket", member.session_ticket);
                        command.Parameters.AddWithValue("@session_key", member.session_key);
                        command.Parameters.AddWithValue("@platform_name", member.platform_name);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [UpdateMember] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public static void UpdateLastOnline(ref Globals.Member member)
        {
            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "UPDATE members SET last_online=@last_online WHERE session_ticket=@session_ticket";
                        command.Parameters.AddWithValue("@last_online", member.last_online);
                        command.Parameters.AddWithValue("@session_ticket", member.session_ticket);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [UpdateLastOnline] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public static bool GetCrewFromCrewId(ref Globals.Crew crew, string crew_id)
        {
            bool result = false;

            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT * FROM crews WHERE crew_id=@crew_id";
                        command.Parameters.AddWithValue("@crew_id", crew_id);

                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                crew.id = reader.GetInt32("id");
                                crew.crew_owner = reader.GetString("crew_owner");
                                crew.crew_id = reader.GetString("crew_id");
                                crew.crew_name = reader.GetString("crew_name");
                                crew.crew_tag = reader.GetString("crew_tag");
                                crew.crew_motto = reader.GetString("crew_motto");
                                crew.crew_color = reader.GetString("crew_color");
                                crew.crew_public = reader.GetInt32("crew_public");
                                crew.crew_invite = reader.GetString("crew_invite");
                                result = true;
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [GetCrewFromCrewId] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

                return result;
            }
        }

        public static bool AttributeExists(string name)
        {
            bool result = false;

            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = "SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME=@table_name AND COLUMN_NAME=@column_name";
                        command.Parameters.AddWithValue("@table_name", "members");
                        command.Parameters.AddWithValue("@column_name", name);

                        return Convert.ToInt32(command.ExecuteScalar()) > 0;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [AttributeExists] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }

                return result;
            }
        }

        public static void SetAttribute(string name, string value, string session_ticket)
        {
            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    using (MySqlCommand command = connection.CreateCommand())
                    {
                        command.CommandText = string.Format("UPDATE members SET {0}=@value WHERE session_ticket=@session_ticket", name);
                        command.Parameters.AddWithValue("@value", value);
                        command.Parameters.AddWithValue("@session_ticket", session_ticket);
                        command.ExecuteNonQuery();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [SetAttribute] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public static string[] FindSession(string sessionTicket, string platformName)
        {
            int memberCount = (int)GetMemberCount(platformName);

            if (memberCount == -1)
            {
                return [string.Empty, string.Empty];
            }

            string[] result = new string[2];

            using (MySqlConnection connection = Create())
            {
                connection.Open();

                try
                {
                    for (int i = 0; i < memberCount; i++)
                    {
                        using (MySqlCommand command = connection.CreateCommand())
                        {
                            command.CommandText = "SELECT * FROM members WHERE id=@id AND last_online > NOW() - INTERVAL 3 MINUTE";
                            command.Parameters.AddWithValue("@id", i);
                            command.ExecuteNonQuery();

                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    if (reader.GetString("session_ticket") == sessionTicket)
                                    {
                                        continue;
                                    }

                                    if (reader.GetInt32("gsjoin") == 1)
                                    {
                                        if (platformName == "ps3")
                                        {
                                            result[0] = reader.GetString("gamertag");
                                        }
                                        else
                                        {
                                            UInt64 ulXuid = UInt64.Parse(reader.GetString("xuid"));
                                            result[0] = ulXuid.ToString("X").TrimStart('0');
                                        }

                                        result[1] = reader.GetString("gsinfo");

                                        return result;
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(string.Format("[DEBUG] [FindSession] Exception: {0}", ex.Message));
                }
                finally
                {
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                }
            }

            return [string.Empty, string.Empty];
        }
    }
}
