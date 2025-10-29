using System.Net;

#nullable disable

namespace GTAServer
{
    public class Globals
    {
        public struct Client
        {
            public HttpListenerRequest request;
            public byte[] requestData;

            public HttpListenerResponse response;
            public byte[] responseData;

            public IPEndPoint endPoint;

            public string method;
            public string path;
        }

        public struct Member
        {
            public int id;
            public string xuid;
            public string gamertag;
            public string crew_id;
            public string crew_tag;
            public DateTime expires;
            public DateTime last_online;
            public string session_ticket;
            public string session_key;
            public int linkdiscord;
            public string discordcode;
            public string discordid;
            public string gsinfo;
            public int gsjoin;
            public int gshost;
            public int banned;
            public string platform_name;
        }

        public struct Crew
        {
            public int id;
            public string crew_owner;
            public string crew_id;
            public string crew_name;
            public string crew_motto;
            public string crew_tag;
            public string crew_color;
            public int crew_public;
            public string crew_invite;
        }

        public static Dictionary<string, Func<Client, Task<int>>> PostList = new Dictionary<string, Func<Client, Task<int>>>()
        {
            { "CreateTicketXbl2", Auth.CreateTicketXbl2 },
            { "CreateTicketNp2", Auth.CreateTicketNp2 },

            { "GetMine", Clans.GetMine },
            { "GetInvites", Clans.GetInvites },
            { "GetMetadataForClan", Clans.GetMetadataForClan },

            { "GetLocationInfoFromIP", GeoLocation.GetLocationInfoFromIP },

            { "GetPresenceServers", Presence.GetPresenceServers },
            { "GetAttributes", Presence.GetAttributes },
            { "SetAttributes", Presence.SetAttributes },
            { "ReplaceAttributes", Presence.ReplaceAttributes },
            { "Query", Presence.Query },
            { "MultiPostMessage", Presence.MultiPostMessage },
            { "Subscribe", Presence.Subscribe },

            { "GetPasswordRequirements", SocialClub.GetPasswordRequirements },
            { "CheckText", SocialClub.CheckText },

            { "GetUnreadMessages", Inbox.GetUnreadMessages },

            { "ReadStatsByGamer2", ProfileStats.ReadStatsByGamer2 },
            { "ReadStatsByGroups", ProfileStats.ReadStatsByGroups },
            { "WriteStats", ProfileStats.WriteStats },
            { "ResetStats", ProfileStats.ResetStats },

            { "SubmitRealTime", Telemetry.SubmitRealTime },
            { "SubmitCompressed", Telemetry.SubmitCompressed },

            { "GetPackValueUSDE", CashTransactions.GetPackValueUSDE },
            { "mpstats", Members.MpStats },
            { "mpchars", Members.MpChars },

            { "QueryContent", Ugc.QueryContent },
            { "CreateContent", Ugc.CreateContent }
        };

        public static string[] GetList =
        {
            "eula_de.xml",
            "tos_de.xml",
            "pp_de.xml",
            "policy_changed_de.xml",

            "eula_en.xml",
            "tos_en.xml",
            "pp_en.xml",
            "policy_changed_en.xml",

            "eula_es.xml",
            "tos_es.xml",
            "pp_es.xml",
            "policy_changed_es.xml",

            "eula_fr.xml",
            "tos_fr.xml",
            "pp_fr.xml",
            "policy_changed_fr.xml",

            "eula_it.xml",
            "tos_it.xml",
            "pp_it.xml",
            "policy_changed_it.xml",

            "eula_ja.xml",
            "tos_ja.xml",
            "pp_ja.xml",
            "policy_changed_ja.xml",

            "eula_ko.xml",
            "tos_ko.xml",
            "pp_ko.xml",
            "policy_changed_ko.xml",

            "eula_mx.xml",
            "tos_mx.xml",
            "pp_mx.xml",
            "policy_changed_mx.xml",

            "eula_pl.xml",
            "tos_pl.xml",
            "pp_pl.xml",
            "policy_changed_pl.xml",

            "eula_pt.xml",
            "tos_pt.xml",
            "pp_pt.xml",
            "policy_changed_pt.xml",

            "eula_ru.xml",
            "tos_ru.xml",
            "pp_ru.xml",
            "policy_changed_ru.xml",

            "eula_zh.xml",
            "tos_zh.xml",
            "pp_zh.xml",
            "policy_changed_zh.xml",

            "version_num.xml",

            "commerceData.xml",
            "ExtraContentManifest.xml",
            "0x1a098062.json",
            "bg_900_0.rpf",

            "1_0.jpg",
            "1_1.jpg",
            "0_0_en.json",

            "save_default0000.save",
            "save_char0001.save",
            "save_char0002.save",

            "emblem_128.dds"
        };
    }
}
