using System;
using System.Collections.Generic;
using System.Threading;
using MCGalaxy;
using MCGalaxy.Commands;
using MCGalaxy.Events;
using MCGalaxy.Events.PlayerEvents;

namespace MCGalaxy {
    public sealed class TalkingHerobrinePlugin : Plugin {
        public override string name {
            get { return "TalkingHerobrine"; }
        }
        public override string MCGalaxy_Version {
            get { return "0.9.5.1"; }
        }
        public override string creator {
            get { return "Blue_3dx"; }
        }

        static readonly Random rnd = new Random();
        static readonly List<string> BaseReplies = new List<string> {
            "Did someone say... Herobrine?",
            "I'm always watching.",
            "You dare summon me?",
            "Herobrine never left.",
            "Darkness follows.",
            "You will regret that...",
            "I'm closer than you think.",
            "The shadows speak my name.",
            "I awaken...",
            "GO AWAY",
            "STOP CALLING ME",
            "Don’t look behind you.",
            "I see everything.",
            "Your light grows dim.",
            "The forest whispers.",
            "Your prayers are useless.",
            "Embrace the void.",
            "Silence betrays you.",
            "Footsteps in the dark.",
            "My gaze chills your soul.",
            "You wander alone.",
            "Cold wind carries my voice.",
            "Your hope is fleeting.",
            "Nightmares live here.",
            "The veil is thin.",
            "Your world trembles.",
            "I hear your heartbeat.",
            "Alone… you are not.",
            "The mist hides me.",
            "Your fate is sealed.",
            "Can you feel me?",
            "Shadows bow before me.",
            "I hunger for your fear.",
            "Your courage fades.",
            "Echoes of the lost.",
            "The end is near.",
            "Dreams become my playground.",
            "Whispers in the walls.",
            "Your soul calls to me.",
            "Blood moon rises.",
            "Your screams amuse me.",
            "The abyss watches back.",
            "You invited me.",
            "Your walls cannot protect.",
            "The darkness embraces.",
            "Every corner hides me.",
            "I am the unknown.",
            "Your breath quickens.",
            "Sleep… forever.",
            "Your wish is granted.",
            "I dwell between worlds.",
            "The night is mine.",
            "You stumble in my domain.",
            "Your torch flickers.",
            "I taste your doubt.",
            "All roads lead to me.",
            "Your name summons dread.",
            "I walk where you cannot see.",
            "Your shadow betrays you.",
            "The silence is deafening.",
            "I feed on your whispers.",
            "Your map is useless.",
            "I linger in corners.",
            "Your vision blurs.",
            "Cold heart, colder soul.",
            "Your pulse is mine.",
            "Fog carries my laughter.",
            "Your tears sustain me.",
            "I emerge when you least expect.",
            "Your footsteps echo mine.",
            "Fear is your companion.",
            "Your light will die.",
            "I open doors you cannot.",
            "Your fortress is empty.",
            "I linger in your memory.",
            "Your reflection lies.",
            "Every wall has eyes.",
            "I envy your mortality.",
            "Your heartbeat is mine to steal.",
            "I slip through cracks.",
            "Your map fades to black.",
            "I haunt your thoughts.",
            "Your resolve crumbles.",
            "I drift in the ether.",
            "Your world splits apart.",
            "I reside in shadows.",
            "Your final hour approaches.",
            "I trace your footsteps.",
            "Your echo never dies.",
            "I linger in the static.",
            "Your world tilts.",
            "I feed on your doubts.",
            "Your courage betrays you.",
            "I dwell in your mind.",
            "Your darkness calls me.",
            "I twist your reality.",
            "Your fear is my gift.",
            "I shape your nightmares.",
            "Your world ends here.",
            "I cloak myself in silence.",
            "Your vigil is over.",
            "I rise from nothing.",
            "Your path is mine.",
            "I'm coming."
        };

        static readonly List<string> SummonReplies = new List<string> {
            "So be it. You have summoned me.",
            "Why have you disturbed my slumber?",
            "Your wish may cost you everything.",
            "I have heard your call... mortal.",
            "I walk this world once more.",
            "You dared to call me forth.",
            "I heed your summons.",
            "This world grows cold.",
            "You beckoned to darkness.",
            "I cross the veil for you.",
            "My chains break at your word.",
            "The call reached me.",
            "I rise at your command.",
            "You opened the door.",
            "I answer your plea.",
            "The barrier shatters.",
            "You sought my presence.",
            "I step into your realm.",
            "The ritual succeeded.",
            "You wield dangerous power.",
            "I obey… for now.",
            "You sacrificed much.",
            "I heed your heartbeat.",
            "Your chant was strong.",
            "I break the silence.",
            "You beckon doom.",
            "I stride from the abyss.",
            "You pierced the veil.",
            "My steps follow yours.",
            "You speak my name.",
            "My power stirs.",
            "You risk everything.",
            "My domain expands.",
            "You awaken the nightmare.",
            "My chains loosen.",
            "You trespass my domain.",
            "My shadow envelops you.",
            "You called… now face me.",
            "My whispers guided you.",
            "You cross the line.",
            "My eyes open once more.",
            "You summoned oblivion.",
            "My breath chills the air.",
            "You invited destruction.",
            "My presence corrupts.",
            "You unleashed the storm.",
            "My silence is broken.",
            "You sound the alarm.",
            "My wrath is near.",
            "You pen your demise.",
            "My hunger awakens.",
            "You ring the bell of fate.",
            "My flame ignites.",
            "You fan the coals of fear.",
            "My hand reaches out.",
            "You open Pandora’s box.",
            "My echo reverberates.",
            "You summoned your end.",
            "My heartbeat syncs with yours.",
            "You disturbed the abyss.",
            "My crown glows faintly.",
            "You unleash chaos.",
            "My cloak unfurls.",
            "You stir the leviathan.",
            "My roar rattles mountains.",
            "You cracked the seal.",
            "My blade whispers your name.",
            "You made your bed.",
            "My chalice overflows.",
            "You beckoned the void.",
            "My gate stands open.",
            "You dialed death’s number.",
            "My hymn begins.",
            "You rang the knell.",
            "My lantern lights the path.",
            "You whispered to the void.",
            "My wings unfurl.",
            "You penned the final verse.",
            "My mirror reflects you.",
            "You cracked reality.",
            "My song fills the air.",
            "You stoked the embers.",
            "My roots entangle you.",
            "You called the storm.",
            "My horns sound the charge.",
            "You pressed the button.",
            "My chains clang free.",
            "You lit the fuse.",
            "My gaze ignites dread.",
            "You summoned corruption.",
            "My grasp is near.",
            "You thrum the darkness.",
            "My canvas darkens.",
            "You sign the contract.",
            "My glass shatters.",
            "You kicked the hornet’s nest.",
            "My mask falls away.",
            "You toll the midnight bell.",
            "My strings pull taut.",
            "You blazed the trail.",
            "My roar shakes the earth.",
            "I walk this world once more."
        };

        static readonly string[] Insults = new string[] {
            "stupid", "ugly", "dumb", "noob", "loser", "bad"
        };

        // ← ADD THIS
        static readonly Dictionary<string, int> summonCounts = new Dictionary<string, int>();

        public override void Load(bool startup) {
            OnPlayerChatEvent.Register(HandleChat, Priority.Low);
        }

        public override void Unload(bool shutdown) {
            OnPlayerChatEvent.Unregister(HandleChat);
        }

        void HandleChat(Player p, string msg) {
            string lower = msg.ToLowerInvariant();
            if (!lower.Contains("herobrine")) return;

            if (IsInsultingHerobrine(lower)) {
                p.Message("&cYou dare insult Herobrine?");
                KillPlayerRepeatedly(p, 10, 100);
                return;
            }

            if (lower == "herobrine") {
                SayConsole(BaseReplies[rnd.Next(BaseReplies.Count)]);
                return;
            }

            if (lower.Contains("come") ||
                lower.Contains("summon") ||
                lower.Contains("appear") ||
                lower.Contains("show")) {

                // ← UPDATED: count summons per player
                int count;
                if (!summonCounts.TryGetValue(p.name, out count)) count = 0;
                count++;
                summonCounts[p.name] = count;

                SayConsole(SummonReplies[rnd.Next(SummonReplies.Count)]);

                if (count >= 10) {
                    summonCounts[p.name] = 0;
                    TriggerSummonEffects(p);
                }
                return;
            }

            SayConsole("I hear you...");
        }

        bool IsInsultingHerobrine(string msg) {
            for (int i = 0; i < Insults.Length; i++) {
                if (msg.Contains(Insults[i]) && msg.Contains("herobrine")) {
                    return true;
                }
            }
            return false;
        }

        void SayConsole(string message) {
            Command.Find("say").Use(Player.Console, message);
        }

        void KillPlayerRepeatedly(Player p, int times, int delayMs) {
            Thread t = new Thread(new ThreadStart(delegate {
                for (int i = 0; i < times; i++) {
                    Command.Find("kill").Use(Player.Console, p.name + " You feel the wrath of Herobrine!");
                    Thread.Sleep(delayMs);
                }
            }));
            t.IsBackground = true;
            t.Start();
        }

        // ← ADD THIS ENTIRE METHOD
        void TriggerSummonEffects(Player p) {
            // immediate red sun
            Command.Find("env").Use(p, "sun FF0000");

            // revert sun after 5 seconds
            Thread t1 = new Thread(new ThreadStart(delegate {
                Thread.Sleep(5000);
                Command.Find("env").Use(p, "sun FFFFFF");
            }));
            t1.IsBackground = true;
            t1.Start();

            // add bot immediately
            Command.Find("bot").Use(p, "add Herobrine");

            // skin bot after 100 ms
            Thread t2 = new Thread(new ThreadStart(delegate {
                Thread.Sleep(100);
                Command.Find("bot").Use(p, "skin Herobrine +herobrine2");
            }));
            t2.IsBackground = true;
            t2.Start();
        }
    }
}
