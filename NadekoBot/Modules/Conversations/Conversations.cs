using Discord;
using Discord.Commands;
using Discord.Modules;
using NadekoBot.DataModels;
using NadekoBot.Extensions;
using NadekoBot.Modules.Conversations.Commands;
using NadekoBot.Modules.Permissions.Classes;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NadekoBot.Modules.Conversations
{
    internal class Conversations : DiscordModule
    {
        private const string firestr = "🔥 ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็ด้้้้้็็็็็้้้้้็็็็็้้้้้้้้็็็็็้้้้้็็็็็้้้้ 🔥";
        public Conversations()
        {
            commands.Add(new RipCommand(this));
        }

        public override string Prefix { get; } = String.Format(NadekoBot.Config.CommandPrefixes.Conversations, NadekoBot.Creds.BotId);

        public override void Install(ModuleManager manager)
        {
            var rng = new Random();

            manager.CreateCommands("", cgb =>
            {
                cgb.AddCheck(PermissionChecker.Instance);

                cgb.CreateCommand("..")
                    .Description("Adds a new quote with the specified name (single word) and message (no limit). | `.. abc My message`")
                    .Parameter("keyword", ParameterType.Required)
                    .Parameter("text", ParameterType.Unparsed)
                    .Do(async e =>
                    {
                        var text = e.GetArg("text");
                        if (string.IsNullOrWhiteSpace(text))
                            return;
                        await Task.Run(() =>
                            Classes.DbHandler.Instance.Connection.Insert(new DataModels.UserQuote()
                            {
                                DateAdded = DateTime.Now,
                                Keyword = e.GetArg("keyword").ToLowerInvariant(),
                                Text = text,
                                UserName = e.User.Name,
                            })).ConfigureAwait(false);

                        await e.Channel.SendMessage("`New quote added.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("...")
                    .Description("Shows a random quote with a specified name. | `... abc`")
                    .Parameter("keyword", ParameterType.Required)
                    .Do(async e =>
                    {
                        var keyword = e.GetArg("keyword")?.ToLowerInvariant();
                        if (string.IsNullOrWhiteSpace(keyword))
                            return;

                        var quote =
                            Classes.DbHandler.Instance.GetRandom<DataModels.UserQuote>(
                                uqm => uqm.Keyword == keyword);

                        if (quote != null)
                            await e.Channel.SendMessage($"📣 {quote.Text}").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage("💢`No quote found.`").ConfigureAwait(false);
                    });

                cgb.CreateCommand("..qdel")
                    .Alias("..quotedelete")
                    .Description("Deletes all quotes with the specified keyword. You have to either be bot owner or the creator of the quote to delete it. | `..qdel abc`")
                    .Parameter("quote", ParameterType.Required)
                    .Do(async e =>
                    {
                        var text = e.GetArg("quote")?.Trim();
                        if (string.IsNullOrWhiteSpace(text))
                            return;
                        await Task.Run(() =>
                        {
                            if (NadekoBot.IsOwner(e.User.Id))
                                Classes.DbHandler.Instance.DeleteWhere<UserQuote>(uq => uq.Keyword == text);
                            else
                                Classes.DbHandler.Instance.DeleteWhere<UserQuote>(uq => uq.Keyword == text && uq.UserName == e.User.Name);
                        }).ConfigureAwait(false);

                        await e.Channel.SendMessage("`Done.`").ConfigureAwait(false);
                    });
            });

            manager.CreateCommands(NadekoBot.BotMention, cgb => {
                var client = manager.Client;

                cgb.AddCheck(PermissionChecker.Instance);

                commands.ForEach(cmd => cmd.Init(cgb));

                cgb.CreateCommand("die")
                    .Description("Works only for the owner. Shuts the bot down. | `@NadekoBot die`")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id)) {
                            await e.Channel.SendMessage(e.User.Mention + ", Yes, my love.").ConfigureAwait(false);
                            await Task.Delay(5000).ConfigureAwait(false);
                            Environment.Exit(0);
                        } else
                            await e.Channel.SendMessage(e.User.Mention + ", No.").ConfigureAwait(false);
                    });

                var randServerSw = new Stopwatch();
                randServerSw.Start();

                cgb.CreateCommand("do you love me")
                    .Description("Replies with positive answer only to the bot owner. | `@NadekoBot do you love me`")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id))
                            await e.Channel.SendMessage(e.User.Mention + ", Of course I do, my Master.").ConfigureAwait(false);
                        else
                            await e.Channel.SendMessage(e.User.Mention + ", Don't be silly.").ConfigureAwait(false);
                    });

                cgb.CreateCommand("how are you")
                    .Alias("how are you?")
                    .Alias("how are you ?")
                    .Description("Replies positive only if bot owner is online. | `@NadekoBot how are you`")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id)) {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as you are here.").ConfigureAwait(false);
                            return;
                        }
                        var kw = e.Server.GetUser(NadekoBot.Creds.OwnerIds[0]);
                        if (kw != null && kw.Status == UserStatus.Online) {
                            await e.Channel.SendMessage(e.User.Mention + " I am great as long as " + kw.Mention + " is with me.").ConfigureAwait(false);
                        } else {
                            await e.Channel.SendMessage(e.User.Mention + " I am sad. My Master is not with me.").ConfigureAwait(false);
                        }
                    });

                cgb.CreateCommand("i love you")
                    .Alias("i love you!")
                    .Alias("i love you !")
                    .Description("WIP")
                    .Do(async e => {
                        // Hardcode on purpose. I'm greedy for attention. ^^
                        if (e.User.Id == 182203552683261952) {
                            await e.Channel.SendMessage(e.User.Mention + " I love you too!").ConfigureAwait(false);
                            return;
                        } else {
                            await e.Channel.SendMessage("Well this chat just became even more awkward...");
                            return;
                        }
                    });

                cgb.CreateCommand("❤")
                    .Alias(" ❤")
                    .Description("WIP")
                    .Do(async e => {
                        if (NadekoBot.IsOwner(e.User.Id)) {
                            await e.Channel.SendMessage("❤ ❤ ❤ ❤");
                            return;
                        } else {
                            await e.Channel.SendMessage("❤");
                        }
                    });

                cgb.CreateCommand("ip")
                    .Alias("ip adress")
                    .Description("Prints bot's LAN IP in chat, for debugging & admin purposes.")
                    .Do(async e => {

                    string ipString = "Error, LAN IP not found! (Somehow :/)";
                    var host = Dns.GetHostEntry(Dns.GetHostName());
                    foreach (var ip in host.AddressList) {
                        if (ip.AddressFamily == AddressFamily.InterNetwork) {
                            ipString = ip.ToString();
                        }
                    }

                    // Taken from stack exchange. It works, so no one asks any questions.
                        string url = "http://checkip.dyndns.org";
                        System.Net.WebRequest req = System.Net.WebRequest.Create(url);
                        System.Net.WebResponse resp = req.GetResponse();
                        System.IO.StreamReader sr = new System.IO.StreamReader(resp.GetResponseStream());
                        string response = sr.ReadToEnd().Trim();
                        string[] a = response.Split(':');
                        string a2 = a[1].Substring(1);
                        string[] a3 = a2.Split('<');
                        string a4 = a3[0];
                        string ipExternalString = a4;
                        if(ipExternalString == null) {
                            ipExternalString = "Error, WAN IP not found! (Somehow :/)";
                        }

                        
                    /*
                    if (ipString == null) { }
                        ipString = "Local IP Address Not Found!");
                    }
                    */
                        // IP detection code runs here.
                        await e.Channel.SendMessage("LAN: " + ipString + "\n" + "WAN: " + ipExternalString).ConfigureAwait(false);

                    });

                cgb.CreateCommand("nmap")
                   .Description("Lists all lan hosts. May take a while to process.")
                   .Do(async e => {

                       string ipString = "";
                       var host = Dns.GetHostEntry(Dns.GetHostName());
                       int n = 0;
                       foreach (var ip in host.AddressList) {
                           ipString += "\n" + n.ToString() + ": " + ip.ToString();
                           n++;
                       }
                       if(ipString == "") {
                          ipString = "Network listing failed, somehow. :/";
                       }

                       await e.Channel.SendMessage("LAN Survey Result:\n" + ipString).ConfigureAwait(false);
                   });

                cgb.CreateCommand("fire")
                    .Description("Shows a unicode fire message. Optional parameter [x] tells her how many times to repeat the fire. | `@NadekoBot fire [x]`")
                    .Parameter("times", ParameterType.Optional)
                    .Do(async e =>
                    {
                        int count;
                        if (string.IsNullOrWhiteSpace(e.Args[0]))
                            count = 1;
                        else
                            int.TryParse(e.Args[0], out count);
                        if (count < 1 || count > 12)
                        {
                            await e.Channel.SendMessage("Number must be between 1 and 12").ConfigureAwait(false);
                            return;
                        }

                        var str = new StringBuilder();
                        for (var i = 0; i < count; i++)
                        {
                            str.Append(firestr);
                        }
                        await e.Channel.SendMessage(str.ToString()).ConfigureAwait(false);
                    });

                cgb.CreateCommand("dump")
                    .Description("Dumps all of the invites it can to dump.txt.** Owner Only.** | `@NadekoBot dump`")
                    .Do(async e =>
                    {
                        if (!NadekoBot.IsOwner(e.User.Id)) return;
                        var i = 0;
                        var j = 0;
                        var invites = "";
                        foreach (var s in client.Servers)
                        {
                            try
                            {
                                var invite = await s.CreateInvite(0).ConfigureAwait(false);
                                invites += invite.Url + "\n";
                                i++;
                            }
                            catch
                            {
                                j++;
                                continue;
                            }
                        }
                        File.WriteAllText("dump.txt", invites);
                        await e.Channel.SendMessage($"Got invites for {i} servers and failed to get invites for {j} servers")
                                       .ConfigureAwait(false);
                    });

                cgb.CreateCommand("ab")
                    .Description("Try to get 'abalabahaha'| `@NadekoBot ab`")
                    .Do(async e =>
                    {
                        string[] strings = { "ba", "la", "ha" };
                        var construct = "@a";
                        var cnt = rng.Next(4, 7);
                        while (cnt-- > 0)
                        {
                            construct += strings[rng.Next(0, strings.Length)];
                        }
                        await e.Channel.SendMessage(construct).ConfigureAwait(false);
                    });

            });
        }



        private static Func<CommandEventArgs, Task> SayYes()
            => async e => await e.Channel.SendMessage("Yes. :)").ConfigureAwait(false);
    }
}
