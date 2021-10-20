//outline
->start

LIST path = (none), scientist, guard, spy, assassin, psychadelic, flygirl, rebel, diplomat, anarchist, battleleader, hacker, deserter

LIST pathblocked = (clear), scientist, guard, spy, assassin, psychadelic, flygirl, rebel, diplomat, anarchist, battleleader, hacker, deserter

//there has got to be a more efficient way to do this jesus rollerblading christ
//I'm allowing multi-paths for now, until it becomes too much of a pain in the ass

//LIST alignment = (none), selfish, evil, herocomplex, chaoticneutral, boringneutral, pacifist, failure

VAR reputation = (0)
//if statement for matching

CONST novazone = 1
CONST outofrange = 2

VAR playerlocation = novazone


LIST questitems = (empty), plasmagun, towel, recondata, cypher, toffee, ctd, coolbomb

VAR science = (1)
VAR computerstuff = (1)
VAR stealth = (1)
VAR charm = (1)
VAR intimidation = (1)
VAR baldfacedliar = (1)
VAR fuckingwithpeople = (1)
VAR pseudospacefuckery = (1)
VAR ingenuity = (1)

VAR CnNjumpin = (3)

LIST companions = (ReenashipAI), Abby, Gman, hitchhiker, ReenaJailbroken

LIST sidecharacters = (none), bossman, katria, uh, add, more, later

LIST worldstates = (none), rogueAI, late, abbydies, gabrindies, HHGonboard, cyphersolved, reinforcementknowledge, katriadies, targeteliminated, doabarrelroll, battleon, infointime, treacherymostfoul, AUinvolved, battlecrewdead, hitchhikerswarning, futurecontact, shipdamaged, squidpropulsionsystem

//reena being jailbroken is a prerequisite for actually getting in coherent contact with Nibu and future you


VAR distractions = (0)
//add faction list

//add abilities? like some ace pilot stuff or a special weapon? deal with it later ahahaha

//add route closed var

===function pathselect(x)
    ~ path += x


===function alter(ref x, y)
   ~ x = x + y
   
===function get(x)
    ~ questitems += x

===function drop(x)
    ~questitems -= x

==start
SPACECAST TIMEEEE
it ya girl
Abby arrives. 
~alter(companions, Abby)
 * get you some plasma guns ->JetMarket
 * no, be responsible ->pathtobattle
 * i've got a better idea -> aiupgradetime 


==JetMarket==
*no password, unceremonious death ->END
*buy some guns! 
    ~get(plasmagun)
    ->JetMarket
*meet shady guy who asks you to use these cool bombs to kill all the corporate drones in the battle
    **accept
      ~alter(path,path.anarchist)
      ~get(coolbomb)
      ->JetMarket
    **refuse
    Congrats, you're a corporate drone! Or just not into mass murder, either or.->JetMarket
*Take a look around, spot a candy booth, and make a beeline for it. 
    ~get(toffee)
    ->JetMarket
  +GET ON WITH IT ->pathtobattle

==aiupgradetime==
Turns out you know some people too. Do a favour for your friend, and they'll show you how to jailbreak your AI copilot.
    *do eeet
    ~alter(science, 1)
    ~alter(companions,-ReenashipAI)
    ~alter(companions, ReenaJailbroken)
    ~alter(worldstates, rogueAI)
    ~alter(path, path.hacker)
    ->pathtobattle
    *oh, no, that sounds illegal! And Reena says that it voids her warranty, and we can't have that.
        ~alter(pathblocked, pathblocked.rebel)
        ->pathtobattle

==pathtobattle==
Finally, you're on your way. But Cat is a magpie, easily distracted by shiny things, and there's so much sparkling between here and there.
Distractions, pick three, only because the author hates twos:
    *{not pathblocked.flygirl}Some shithead flyboy cut you off! The only solution is a race...to the death!->crashtestdummies
    *{not pathblocked.psychadelic}A hitchhiker hails you. You know they weren't there three seconds ago. You invite them aboard and detour to their destination...and perhaps your destiny. Jokes on you, you don't have one. ->hitchhikesguide
    *{not pathblocked.spy} receive a coded hail with a legerdomaine passphrase or something like that from a damaged ship. Answer it.->espionage
    *{not pathblocked.assassin}receive a private message from a hooded mystery figure claiming to represent a very select consortium of corporate assassins. They ask you to prove your ability to murder without compunction, and then you can talk about a lucrative contract.->assassination
    *turn-counter to start the battle. 
    ->battle
    
=crashtestdummies
Race through some wildly dangerous space terrain. It's fun. You probably died. What a day. Abby is recording everything on her dashcam.
    *win the race
        ~alter(worldstates,doabarrelroll)
    *lose the race
        ~alter(pathblocked, pathblocked.flygirl)
        
    
-(yo) well then. 
{doabarrelroll:
        your reckless disregard for life and limb--mostly other people's--has paid off. You've been invited to join the super elite Crash Test Dummy club! hooray!
            *accept!
                ~alter(path, path.flygirl)
                ~get(ctd) 
                ->pathtobattle
    
            *Yeah...these guys are a bunch of fuckheads. Later, bitches.
                ~alter(pathblocked, pathblocked.flygirl) 
                ->pathtobattle
    -else:
    ->pathtobattle
}

===hitchhikesguide===
Abby bails on you, because seriously, Cat? They're not even paying you!
~ companions -= Abby
~companions += hitchhiker
some wild pseudospace stuff. You get some new pseudospace based ability? let me think about what exactly that may be.
~alter(path,path.psychadelic)
~alter(pseudospacefuckery,1)
~get(towel)
{ rogueAI:
    Reena and your new friend have an interesting discussion. Your AI is now, apparently, a communist.
    ~alter(path,path.rebel)
    }
->pathtobattle

===espionage===
Spy stuff? Spy stuff. The damaged ship--with one survivor unlikely to maintain that status for much longer--was sent on a recon mission in preparation for the battle. Obviously, it was not a rousing success. Recognizing an ally in your MT ship codes (and your legerdomaine origins)--for the time being, anyway--they entrust you with their information, and the goal of their mission. It is, of course, in code.
    ~get(recondata)
    *take information directly to your bosses
        They thank you politely, toss you a few credits, and suggest you forget all about this. 
            ~alter(pathblocked, pathblocked.spy)
            ~drop(recondata)
            ->pathtobattle
    *Ciphers! You fucking love ciphers! Attempt to solve in minigame. Be warned, though...you only get three tries before the message erases itself.
        **C-C-C-CODEBREAKER! Well done. Return to your superiors with the now decoded message. Someone in the bureaucracy decides that, as you apparently have the skills, initiative, and aren't stopped by codes, they may as well use that rather than go through all the trouble of removing you for knowing too much. Head for the battle, and await your new orders. 
            ~alter(worldstates, cyphersolved)
            ~drop(recondata)
            ~alter(ingenuity, 1)
            ->espionageredux
        **WHOOPS! You done fucked up.
             ~alter(pathblocked, pathblocked.spy)
            ~drop(recondata)
                 ***give undecoded message to your bosses and don't mention your failed attempts at decoding it. Get a few credits and a dire warning. ->pathtobattle
                ***let's just...forget all about this, yeah? I'm sure it wasn't important.->pathtobattle
    *Well, you suck at ciphers, but you're pretty good at espionage. Someone must have the key, and you're going to find them and "unobtrusively borrow" it. You wouldn't want to bother anyone, after all.
        Fly into Zhestokost territory and track down someone with the knowledge. Details to be added later.
            **successfully obtain the knowledge. Yay! Go return the decoded message, get hired as a spy. It even comes with healthcare!
                ~alter(worldstates, cyphersolved)
                ~drop(recondata)
                //alter stealth, charm, intimidation, fucking with people, whatever, depending on how this played out
                ->espionageredux
            **You really suck at this, huh?
                 ~alter(pathblocked, pathblocked.spy)
                ~drop(recondata)
                ***give undecoded message to your bosses and don't mention your failed attempts at decoding it.
                      ~alter(pathblocked, pathblocked.spy)
                     ~drop(recondata)
                    ->pathtobattle
                ***let's just...forget all about this, yeah? I'm sure it wasn't important.->pathtobattle

    =espionageredux
    ~alter(path,path.spy)
    ~alter(pathblocked, pathblocked.guard)
    ~alter(pathblocked, pathblocked.battleleader)
    ~alter(worldstates, battleon)
    The battle is on! Fortunately, with your shiny new job, you don't have to get involved in the messy melee bit. You get to break into a highly secured zone with a top tier defense system and find out what Finch(?) is doing there. Isn't that so much better?
            {charm >= 3}{intimidation >=3}{baldfacedliar >=3}
                You convince/threaten/straight up lie, so you can take your best wingbuddy along into enemy territory on a top secret mission, even though they're the most unreliable mfer ever and can't keep a secret to save their life.
            {charm <= 2}{intimidation <=2}{baldfacedliar <=2}
            Top secret, covert ops, etc etc, which means...your buddy has to stay behind. Your superiors know Abby. Everyone knows Abby! That's a problem when you're being sneaky. 
            //adjust this for multiple companion options
                ~alter(companions, -Abby)
        *U ded. Whoops. ->END
        *you got caught before you learned anything. Nice going. Your choices now are treachery or death.
        ~alter(worldstates, treacherymostfoul)
        ->traitor
        *Turns out Finch is secretly massing reinforcements for Zhestokost. Time to get that information back to base.
            ~alter(worldstates,reinforcementknowledge)
            { companions ? Abby:
                Welp, it's your friend or the mission. What do you choose?
                ** Why not both?	
                    { path ? path.flygirl:
		                 Fortunately, you're an ace pilot. Save ya girl and get back in time to save the day as well.
		                    ~alter(worldstates,infointime)
		                    ->battle
                    - else:
		                  Uh...you may have fucked up here. You can't save both, and trying to do it means you manage neither.
		                  ~alter(worldstates, abbydies)
		                  ->battle
		            }
	           **My friend, obvs.
	             { path ? path.flygirl :
		                 Fortunately, you're an ace pilot. Save ya girl and get back in time to save the day as well.
		                    ~alter(worldstates,infointime)
		                    ->battle
		           -else: 
		            You saved your friend! Great job. Buuuut you didn't get the info back in time. Now the rest of your friends are dead. Good going!
		                ~alter(worldstates, battlecrewdead)
		                ->battlestate3
		           }
	           **The mission comes first, the greater good, yadayadayada
	                ~alter(worldstates,abbydies)
	                ~alter(worldstates,infointime)
	                ->battlestate3

               -else:
                get you back to the battle with your info, no dramatic choices required. 
                ~alter(worldstates, infointime)
                ->battlestate3
             }  
                    
        =espionage3
{battle.doublecross}
Damn, you're good at this. Your handlers have a new mission for you! And this one's a doozy. 
A double agent has stolen some important data--and do they tell you what it is? They sure don't. They've fled into Adrasteia territory, and it's your job to find them and eliminate the threat.
Imagine your surprise when on catching up to them, you get a hail from your old frenemy, Gavrin. You really never imagined he had the guts. He offers to split the payment if you help him with his job and don't kill him.
Turns out the info he was apparently given rather than stole was the location of a 'phage creche, and he's been sent to snatch a cocoon and bring it back to...Miss Terri's?
(You were right. He doesn't have the guts for treachery.)
    *Who cares? You're getting paid to eliminate the threat, and eliminate the threat you will. Shoot him down like the dog he is, and return triumphant.
    *Try to untangle exactly how everyone ended up here, because Gabrin sure doesn't know and you're pretty sure the same people who sent you to kill him sent him on his own mission. Which is...suspicious, if nothing else.
    *Accept the offer.
    

===battle===
{path ? path.battleleader}
WHOOO! It's fight time! And if you aren't too busy committing bloody murder or being an international cat of mystery, it's time to engage with the enemy.
 
    *Use your powers for evil--and profit! ->doublecross
    * {path ? path.guard} You've already got a job! Protect Katria! ->escortmission
    *Use your piloting skills and aresenal of highly destructive weaponry! ->tankitup



  
   
    =doublecross
        lsaksdjfaslkjd
   
    =escortmission
   //assassin faction reputation tanked, yada yada, death squad puts a price on your head, blah blah blah
    You can only assume your erstwhile employers haven't given up on their intentions to eliminate Katria. Your job now is to outthink, outflank, and then take out your replacement, and not for a nice dinner on the ice moons of Erzatz 5.
    *You failed, and now your friend is dead and a whole syndicate of very murdery people are out for your head.
        ~alter(worldstates, katriadies)
        ->battlestate3
    *You did it! 
        {companions ? ReenaJailbroken} ->NOVATIMEEE.outtahere
   
    =tankitup
    no prerequisites! Hooray.
    ~alter(path, path.battleleader)
    *{path ? path.flygirl && questitems ? plasmagun && charm >= 3} 
    You've got the charisma, you've got the skills, and you've got the highly unstable black market weaponry! Rally the troops and lead them to victory...for the moment. ->battlestate3
    *{path ? path.flygirl && questitems ? plasmagun}
        You've got the skills, you've got the weaponry...you don't have the charisma, but that's okay. Other people just get in your way anyhow. ->battlestate3.shiprepairs
            ~alter(worldstates, shipdamaged)
    *{path ? path.flygirl}
        You've got the skills! And that's about it. Fortunately, it might just be enough to save the day. Just...maybe not your friends as well.
        ~alter(worldstates, abbydies)
        ~alter(worldstates, shipdamaged)
        ->battlestate3.shiprepairs
    *{charm >=7}
        Well, you don't have the skills, or the weaponry, but you do have an incredible gift for manipulation. Rally the troops, and lead them to a horrible but spectacular death!
        ~alter(worldstates, abbydies)
        ~alter(worldstates, shipdamaged)
        ~alter(worldstates, battlecrewdead)
        ->battlestate3.shiprepairs
    
    ==battlestate3==
    Righto. You've vanquished at least a few of the foe. Now it's time to...get some new foes? Yep, Lucent just switched sides. Motherfuckers. 
   -(superspy) 
   {worldstates ? infointime}
    Fortunately, it's only Lucent you're dealing with, because thanks to your super spy shenanigans, the Finch reinforcements have been taken out of play.
   -(spyfail) 
   {worldstates ? reinforcementknowledge && not infointime && not treacherymostfoul}
    There's also the matter of the incoming Finch reinforcements, who, thanks to your poor decision making, are already on the scene and have taken out most of your squadron. Which means you've got a whole heaping helping of trouble on your hands. At least you're forewarned?

    -(nonespy)
    {path ? path.spy}
    Surprise! Finch is joining the party, and they brought snacks. By which, of course, we mean fresh troops and a lot of high tech weaponry. Yeah, whoever they sent instead of you kinda fucked up big time.
   
 *{path ? path.hacker && companions ? ReenaJailbroken}
            {battlestate3.spyfail || battlestate3.nonespy}
            The notable thing about Finch ships, beside the a e s t h e t i c, is the super advanced, super breakage-prone tech. More moving parts, more failure points, etc. Fortunately, Reena knows this too, and on the way back from your failed mission, she's been working on a solution to take them out. Pitch in with your own expertise, and deploy a virus right into the Finch mainframe.
            {superspy}
            Since you don't have to worry about Finch, you have the leisure to crash the Lucent streaming party! Hijack the broadcast and the limelight.
 *{science >= 5 && questitems ? ctd} Use your wits and your non-AI copilot to take out more than your fair share of ships. ->gravityslingshot
*{worldstates ? squidpropulsionsystem}
        You've got the element of suprise on your side. Charge into battle, immune to any EMP grenades, and wreak havoc. 
    
        =shiprepairs
            Unfortunately, you took damage. Lots of it! Your thrusters and weaponry are out of commission. Now what?
            *{path ? path.flygirl && pseudospacefuckery >= 3 && questitems ? towel}
                Some people might attempt repairs, some people might give up, some people might look to their friends for help, but you...you know the only real course of action is to whistle up a space squid and lasso that fucker to pull your ship back into battle like unto a celtic warrior queen of old. Good thing you brought your towel!
                ~alter(worldstates, squidpropulsionsystem)
                ->battlestate3
            *{science >= 7}
                Your ship may be reduced to a heaping hunk of space junk, but not your brain. Use those engineering skills to fix up your ship and get right back to it. 
                ~alter(worldstates, -shipdamaged)
                ->battlestate3
            *{ingenuity >=5 && questitems ? toffee}
            Well, you're no mechanic, but you've got brains, a copy of the ship schematics, and a pocket full of extremely sticky candy. This shouldn't be too hard, right?
            ~alter(worldstates, -shipdamaged)
            ~drop(toffee)
            ->battlestate3
            *{not abbydies}
            You're no mechanic, but you don't need to be, because your best wingbuddy is the queen of the machine shop. Radio for assistance!
             ~alter(worldstates, -shipdamaged)
             ->battlestate3
            *Some may be discouraged in a situation like this. Not you! Unassailable odds won't stop you, because words like "unassailable" are way beyond your reading ability. So take a look at the damage and give it your best shot. Or not. It'll end up the same either way. An excrutiating death! ->END
  =gravityslingshot
    jkljk           
===assassination===
{not pathblocked.assassin}
Prove your stealth, craftiness, and homicidal bloodlust. Take out an AU ship carrying someone presumably important in their home territory, <i>without</i> causing a diplomatic incident.
    *{not abbydies && science >=3} Blinding people with science is easy! Murdering them? Also easy, and fun. And luckily, Abby knows a guy who knows an AI who works in the Finch Corporate Terrorism labs. It's mad scientist time!
        Take a look, and pick your favourite!
            **A biotech neural net virus designed originally as spyware. It’s not revolutionary or particularly unusual as far as that goes. It does, however, due to the interconnectedness of squishy flesh brain and tech that creates the neural net, have the fun side effect of causing proteins in the brain to straight up melt. Phenomenal! 
                {science >= 5 && worldstates ? rogueAI && path ? path.hacker:
                    Also luckily, you're a master of the scientific method, and you've got a jailbroken AI just raring for the opportunity to crack some digital copyrights, because a scan of the patent informs you of a failsafe in the protein's amino acid sequence that requires a particular Finch-copyright bit of code to be added before the brain melty bit will activate. And there's a second failsafe, which requires a different bit of code and stops the virus from initiating an explosive self-destruct sequence on the source computer than downloading into the target's net. Steal that code, and the virus, and go kill thy enemy with reckless disregard for any morality whatsoever. 
                        ~alter(science, 1)
                        ~alter(ingenuity, 1)
                        ->assasination2
                    -else:
                    u done fucked up! self destruct sequence activated.->END
                    }
            **Poison? A classic. A few drops of this stuff will have your target writhing on the ground as their insides turn into their outsides! But how the heck do you plan to get it into your enemy's morning XCaff?
                ***Use your dubious charms and Abby's connections to convince an AU attache to do the deed. The meet to give them the poison is in a surprisingly tricky to reach area.
                    ****{charm <=3} IT'S A TRAP!
                        //check is also for fucking with people and baldfaced liar. Intimidation, in this situation, gets you nowhere.)
                        ~alter(pathblocked, pathblocked.assassin) 
                        ->pathtobattle
                    ****{charm >= 3} It's not a trap! Yay. Give them the poison, sit back, and enjoy the soothing chiptune tones of the lollipop choir and hi-def broadcast screaming.
                        ~alter(fuckingwithpeople, 1)
                        ~alter(charm, 1)
                        ->assasination2
                **Genetically engineered viruses are your jam! And you can always count on them to get you out of one. This one is highly infectious, is grafted onto a tardigrade and can survive vacuum conditions, and kills in half an hour. Implausible? Sure. But then, so are you.  
                    Now all you need to do is get close enough to deploy your new monocellular best friend into the air vents of your target ship.
                    CUE MINIGAME WHERE YOU DO A FLYBY AND DROP THE VIRUS INTO SOME SUSPICIOUSLY CONVENIENT OPENING
                        ***You did it! Good job, you bioterrorist, you.
                        ~alter(stealth, 1)
                        ~alter(science, 1)
                        ->assasination2
                        ***FAILURE. Whoops.
                            ~alter(pathblocked, pathblocked.assassin)
                            ->pathtobattle
//Maybe we give the player a couple of points to allot at the beginning? Just to make the skill checks more checky, if you follow.
    *{science >= 3 && path.hacker}
        This seems like the opportune time to deploy your computer skills. Write or find a virus, and then get close enough, undetected, to your target to upload your code onto their servers and kill their power. For bonus points, make it look like someone on board just accidentally bumped the big red button and set off a shut-down sequence.
            **Mission accomplished!
            ~alter(stealth, 1)
            ~alter(computerstuff, 1)
            ->assasination2
            **UPLOAD FAILURE, though undetected 
             ~alter(pathblocked, pathblocked.assassin)
            ->pathtobattle
            **You were spotted! And unlike the Etricarian shifting leopard, you can't change them. Whoops!
              ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->pathtobattle
            
    *{path.diplomat} and charm intimidation or etc at a reasonable level
    Shady backroom deals and political skullduggery? You're there. Use your connections to machiavelli whoever you hate most into committing the murder for you.
        **Success! 
        ~alter(fuckingwithpeople, 1)
        ~alter(charm, 1)
        ->assasination2
        **You got outmaneuvered. Uh-oh.
            ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->pathtobattle
    *{ingenuity >= 3 && questitems ? coolbomb}
        You're pro fuckoff-awesome anti-matter bombs. You're anti notoriety. Antithetical? Absolutely not. Just use that handy dandy explosive your terrorist friend gave you and leave some flashing neon arrows towards whoever you hate most.
            **You're not as clever as you think you are. 
            ~drop(coolbomb)
            ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->pathtobattle
            **You might be almost as clever as you think. Nice going.
            ~drop(coolbomb)
            ~alter(ingenuity, 1)
            ->assasination2
    *{pseudospacefuckery >= 5 && ingenuity >=3 && fuckingwithpeople >=3}
        You <i>could</i> kill someone the sneaky way, or the high-tech way, or the old-fashioned bloodbath way...but that's <i>boring</i>. {questitems ? towel} Luckily, you brought your towel. You don't need it for your plan, but it's nice to be prepared.
        Hail your target ship, insult the captain until they attack, and lead them on a merry pseudospace chase into candyland where your connection to all things uncanny and improbable lets you whistle up a taffy creature into body slamming your pursuit.
            ~alter(pseudospacefuckery, 1)
            ~alter(fuckingwithpeople, 1)
            ~alter(ingenuity, 1)
            ->assasination2
    *{stealth >= 3}
    Space dogfights are fun and all, but stealth is your pianissimo, you put the silent 'b' in subtlety, you're wearing custom InCognito noise-damper boots and three layers of aliases, you--well, you're much too sneaky to be going around boasting about it. Instead, don your cape of invisibility, commit bloody murder, and saunter away without a spot of gore on you.
        **Someone saw you! Turns out it's harder than you thought to fly under the radar in four dimensions.
              ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->pathtobattle
        **Success! Nice.
            ~alter(stealth, 3)
            ->assasination2
    *You're not really good at anything, but you're willing to make an effort. Go in guns blazing, and fail spectacularly.
        **u ded->END
        **You did it! And you also tanked intercorp relations for the next decade. Nice.
            ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->battle
            
    =assasination2
    ~alter(path, path.assassin)
    ~alter(worldstates, battleon)    
        You've proved your worth to the mysterious syndicate of highly-paid murderers. Nice. Now the real fun begins. Your new mission? A Very Important Person with an allegedly excellent security escort is on hand to enjoy the carnage. Get thee to a gunnery and blow them to hell.
        *This seems like the opportune time for a little subterfuge. And what better way to get past someone's guard than to <i>be</i> their guard? Join the battle, swoop in to save the VIP from a suspciously convenient danger, and get promoted to bodyguard extraordinaire. And actually, as it happens, your new client, Katria Kas, is...cool as hell. And you're name twinsies! Uh oh.
          
            **Fuck all that shady assassin business, you've got a new friend and forewarned is forearmed, after all. And luckily for you and them, you're the galactic arm wrestling champion.
                ->battle.escortmission
                ~alter(path, path.guard)
                ~alter(path, -path.assassin)
                ~alter(pathblocked, pathblocked.assassin)
            **Cool or not, they've got a contract out on their head, and you're contractually obligated to hold to it. Anyway, they didn't recruit you to kill people because you've got moral scruples about it. 
                ~alter(worldstates, katriadies)
                ~alter(pathblocked, pathblocked.guard)
                ->Assassin3
            
    =Assassin3
    alkdsjfalskdjflaks


          
          
          
           
===traitor===
aksdjfhasldj;ha
    

    
    
===NOVATIMEEE===    
   // endings to be adjusted to final draft, obvs
    =outtahere
    And now...future you and some insane future AI who Reena assures you is like, super cool, seriously, are sending you messages telling you to get the hell out of dodge before something happens. It would be nice if they'd told you what, but apparently you're about to find out. Grab your friends and your ship, make like a tree, and leaf->END
    
    =paradoxbox
    //pseudospace flygirl rebel ending 
    something something future you, something something pseudospace anomaly, something something hitchhiker's warning. Plot twist! The mysterious hitchhiker is Nibu in a false mustache and glasses!->END
    
    =chargeofthelightbrigade
    //if player hasn't communicated with our future characters, and is in the battle zone at nova time
    {playerlocation == novazone && not futurecontact}
    Caught in the nova, you survive by a freak chance. It is not a freak chance, but you don't know that, because you've apparently been living under an asteroid.->END

    =chargebutbetter
    {playerlocation == novazone && worldstates ? futurecontact}
    You like making things difficult for your future self, don't you? Your dumb ass is still in range of the explosion! Luckily, future!cat and deci know that you're an idiot, and have prepared for this possibility. Don't worry, you'll regret it later when Deci gets their hands on you. ->END
    
    =obliviousoctopus
    {playerlocation == outofrange && not futurecontact}
    You're none too bright, but you do know that when the mysterious forces who've been taking over and saving you this whole time keep trying to steer your ship the hell away, you should probably let them. 
    ->END
    
    
    
    //path interactions: flygirl + psychadelic + stickytoffee leads to squid lassoing; spy + assassin gets you a mad stealth booth and a reputation; hacker+rebel gets you the ability to crash the holomeeting, diplomat + spy gets you critical info and the chance to blow some shit up, deserter + psychadelic gets your hitchhiker friend sending you back to the battle over destiny or some shit, deserter + assassin adds an extra element of treachery when your fellow death squad members come after you, diplomat + assassin opens the option for strategic murder, flygirl + spy allows you to both save Abby and get back to base with your info in time, psychadelic+flygirl+rebel is the Ultimate Madness level, highly recommended, psychadelic+assassin lets you do the adrasteia realm mission in the most dramatic manner, anarchist + anything absolutely tanks your reputation and puts a bounty on your head, flygirl + battleleader + questitem plasma gun lets you lead an actually successful charge, diplomat + spy + psychadelic allows you to actually negotiate a peace--right before everything goes nova, and then get blamed for double crossing everyone. Scientist + flygirl lets you pull a fabulous gravity slingshot maneuver, and launch your crash test dummy strapped with an antimatter bomb riiiight at the weak spot of the ZK boss ship. Scientist + assassin means it's bioweapon time, assassin + hacker is a virus download. I will cull or add to these as I see fit. guard + assassin opens a doublecross path.
    
//the deserter path inevitably ends in either the collapse of the universe in a huge paradox or future!Cat and Nibu basically forcing you back to the battle. This will use up all your jump-ins. Either way, it does not put you in a good position going forward.
    
    
    
    
