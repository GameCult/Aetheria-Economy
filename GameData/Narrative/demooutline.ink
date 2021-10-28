//outline
->chooseyourpowers

LIST path = (none), scientist, guard, spy, assassin, psychadelic, flygirl, rebel, diplomat, anarchist, battleleader, hacker, deserter

LIST pathblocked = (clear), scientist, guard, spy, assassin, psychadelic, flygirl, rebel, diplomat, anarchist, battleleader, hacker, deserter

//there has got to be a more efficient way to do this jesus rollerblading christ
//I'm allowing multi-paths for now, until it becomes too much of a pain in the ass

//LIST alignment = (none), selfish, evil, herocomplex, chaoticneutral, boringneutral, pacifist, failure

VAR reputation = (0)
//if statement for matching
VAR fogofwar = (0)
//what does this variable even mean for fucks sake em

CONST novazone = 1
CONST outofrange = 2

VAR playerlocation = novazone
VAR credits = (0)

LIST questitems = (empty), plasmagun, towel, recondata, cypher, toffee, ctd, coolbomb, phagecocoon, finchspyware

VAR science =  0
VAR computerstuff = 0
VAR stealth = 0
VAR charm = 0
VAR intimidation = 0
VAR baldfacedliar = 0
VAR fuckingwithpeople = 0
VAR pseudospacefuckery = 0
VAR ingenuity = 0

VAR pointallotment = (0)

VAR CnNjumpin = (3)

LIST companions = (ReenashipAI), Abby, Gman, hitchhiker, ReenaJailbroken, katria

LIST worldstates = (none), rogueAI, late, abbydies, gabrindies, HHGonboard, cyphersolved, reinforcementknowledge, katriadies, targeteliminated, doabarrelroll, battleon, infointime, treacherymostfoul, AUinvolved, battlecrewdead, hitchhikerswarning, futurecontact, shipdamaged, squidpropulsionsystem, lucentswitcheroo, spycomplete, assassincomplete, takeupthemantle, totalfuckhead

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

==function say(x)
~return x



===chooseyourpowers===
You have 9 points. Pick wisely.
-(choices)
    +{pointallotment <=8} Science!
        Science increased by 1!
        ~alter(science, 1)
        ~alter(pointallotment, 1)
        Science: {science}
        ->choices
    +{pointallotment <=8} Hacking
        Hacking increased by 1! 
                ~alter(computerstuff, 1)
        ~alter(pointallotment, 1)
        Hacking: {computerstuff}
        ->choices
    +{pointallotment <=8} Sneaky bastard
        Stealth increased by 1! 
        ~alter(stealth, 1)
        ~alter(pointallotment, 1)
        Stealth: {stealth}
        ->choices
    +{pointallotment <=8} Charm
        Charm increased by 1! 
        ~alter(charm, 1)
        ~alter(pointallotment, 1)
        Charm:{charm}
        ->choices
    +{pointallotment <=8} Intimidation
        Intimidation increased by 1! 
        ~alter(intimidation, 1)
        ~alter(pointallotment, 1)
        Intimidation: {intimidation}
        ->choices
    +{pointallotment <=8} Fucking with people's heads
        Troublemaking increased by 1! 
        ~alter(fuckingwithpeople, 1)
        ~alter(pointallotment, 1)
        Troublemaking:{fuckingwithpeople}
        ->choices
    +{pointallotment <=8} Pathological liar
        Poker face increased by 1! 
        ~alter(baldfacedliar, 1)
        ~alter(pointallotment, 1)
        Poker Face: {baldfacedliar}
        ->choices
    +{pointallotment <=8} Pseudospace Witchcraft
        Pseudospacecraft increased by 1! 
        ~alter(pseudospacefuckery, 1)
        ~alter(pointallotment, 1)
        Pseudospacecraft: {pseudospacefuckery}
        ->choices
    +{pointallotment <=8} Ingenuity
        Ingenuity increased by 1!
        ~alter(ingenuity, 1)
        ~alter(pointallotment, 1)
        Ingenuity: {ingenuity}
        ->choices
    *{pointallotment >= 9}
        All set! Now get on with it. ->start

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
    ~companions -= ReenashipAI
    ~alter(companions, ReenaJailbroken)
    ~alter(worldstates, rogueAI)
    ~alter(path, path.hacker)
    Yay! Now you've got a suspiciously familiar and very opinionated AI.
        **Get thee on your way!
        ->pathtobattle
    *oh, no, that sounds illegal! And Reena says that it voids her warranty, and we can't have that.
        ~alter(pathblocked, pathblocked.rebel)
        ->pathtobattle

==pathtobattle==
Finally, you're on your way. But Cat is a magpie, easily distracted by shiny things, and there's so much sparkling between here and there.
Distractions, pick three, only because the author hates twos:
    *{pathblocked != pathblocked.flygirl && TURNS_SINCE(->pathtobattle) <= 3} Some shithead flyboy cut you off! The only solution is a race...to the death!->crashtestdummies
    *{pathblocked != pathblocked.psychadelic && TURNS_SINCE(->pathtobattle) <= 3} A hitchhiker hails you. You know they weren't there three seconds ago. You invite them aboard and detour to their destination...and perhaps your destiny. Jokes on you, you don't have one. ->hitchhikesguide
    *{pathblocked != pathblocked.spy && TURNS_SINCE(->pathtobattle) <= 3}  receive a coded hail with a legerdomaine passphrase or something like that from a damaged ship. Answer it.->espionage
    *{pathblocked != pathblocked.assassin && TURNS_SINCE(->pathtobattle) <= 3} receive a private message from a hooded mystery figure claiming to represent a very select consortium of corporate assassins. They ask you to prove your ability to murder without compunction, and then you can talk about a lucrative contract.->assassination
    *{TURNS_SINCE(->pathtobattle) >= 4}turn-counter to start the battle. 
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
            They give you your official crash test dummy, and you strap in and head back off to work.
                CRAST TEST DUMMY added to inventory.
                ~alter(path, path.flygirl)
                ~get(ctd) 
                ->pathtobattle
    
            *Yeah...these guys are a bunch of fuckheads. Later, bitches.
                ~alter(pathblocked, pathblocked.flygirl) 
                ->pathtobattle
    -else:
    You'd better get back. You're way too slow to make it otherwise, after all.->pathtobattle
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
    *Alright, back to your battle. ->pathtobattle

===espionage===
Spy stuff? Spy stuff. The damaged ship--with one survivor unlikely to maintain that status for much longer--was sent on a recon mission in preparation for the battle. Obviously, it was not a rousing success. Recognizing an ally in your MT ship codes (and your legerdomaine origins)--for the time being, anyway--they entrust you with their information, and the goal of their mission. It is, of course, in code.
    ~get(recondata)
    *take information directly to your bosses
        They thank you politely, toss you a few credits, and suggest you forget all about this. 
            ~alter(pathblocked, pathblocked.spy)
            ~drop(recondata)
            **back to business!->pathtobattle
    *Ciphers! You fucking love ciphers! Attempt to solve in minigame. Be warned, though...you only get three tries before the message erases itself.
        **C-C-C-CODEBREAKER! Well done. Return to your superiors with the now decoded message. Someone in the bureaucracy decides that, as you apparently have the skills, initiative, and aren't stopped by codes, they may as well use that rather than go through all the trouble of removing you for knowing too much. Head for the battle, and await your new orders. 
            ~alter(worldstates, cyphersolved)
            ~drop(recondata)
            ~alter(ingenuity, 1)
            ***Next mission?->espionageredux
            ***Let's start back to the battle. ->pathtobattle
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
                ***Next mission!->espionageredux
                ***Hmm...better get back. ->pathtobattle
            **You really suck at this, huh?
                 ~alter(pathblocked, pathblocked.spy)
                ~drop(recondata)
                ***give undecoded message to your bosses and don't mention your failed attempts at decoding it. You've got stuff to do anyway.
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
        *U ded. Whoops.
            {CnNjumpin < 3:
                -*save yourself!
                ~alter(CnNjumpin, 1)
                ->->
            -else:
                ->END
            }
        *you got caught before you learned anything. Nice going. Your choices now are treachery or death.
        ~alter(worldstates, treacherymostfoul)
                **Treachery, of course.->traitor.spypath
                **Death! ->END
        *Turns out Finch is secretly massing reinforcements for Zhestokost. Time to get that information back to base.
            ~alter(worldstates,reinforcementknowledge)
            { companions ? Abby:
                Welp, it's your friend or the mission. What do you choose?
                ** Why not both?	
                    { path ? path.flygirl:
		                 Fortunately, you're an ace pilot. Save ya girl and get back in time to save the day as well.
		                    ~alter(worldstates,infointime)
		                    	 ***Next mission! ->espionage3
	                             ***Go check on the battle->battle
                    - else:
		                  Uh...you may have fucked up here. You can't save both, and trying to do it means you manage neither.
		                  ~alter(worldstates, abbydies)
		                  ->battle
		            }
	           **My friend, obvs.
	             { path ? path.flygirl :
		                 Fortunately, you're an ace pilot. Save ya girl and get back in time to save the day as well.
		                    ~alter(worldstates,infointime)
	                ***Next mission! ->espionage3
	                ***Go check on the battle->battle
		           -else: 
		            You saved your friend! Great job. Buuuut you didn't get the info back in time. Now the rest of your friends are dead. Good going!
		                ~alter(worldstates, battlecrewdead)
	                ***Next mission! ->espionage3
	                ***Go check on the battle->->battle->->
		           }
	           **The mission comes first, the greater good, yadayadayada
	            Abby dies, but you get that info back in time!
	                ~alter(worldstates,abbydies)
	                ~alter(worldstates,infointime)
	                ***Next mission! ->espionage3
	                ***Go check on the battle->->battle->->

               -else:
                get you back to the battle with your info, no dramatic choices required. 
                ~alter(worldstates, infointime)
                	 ***Next mission! ->espionage3
	                ***Go check on the battle->->battle->->
             }  
                    
        =espionage3
        ~alter(worldstates, lucentswitcheroo)
Damn, you're good at this. Your handlers have a new mission for you! And this one's a doozy. 
A double agent has stolen some important data--and do they tell you what it is? They sure don't. They've fled into Adrasteia territory, and it's your job to find them and eliminate the threat.
Imagine your surprise when on catching up to them, you get a hail from your old frenemy, Gavrin. You really never imagined he had the guts. He offers to split the payment if you help him with his job and don't kill him.
Turns out the info he was apparently given rather than stole was the location of a 'phage creche, and he's been sent to snatch a cocoon and bring it back to...Miss Terri's?
(You were right. He doesn't have the guts for treachery.)
    *Who cares? You're getting paid to eliminate the threat, and eliminate the threat you will. You always hated gabrin anyway. Shoot him down like the dog he is, and return triumphant.
        ~alter(worldstates, gabrindies)
        **...You may well go ahead and grab that 'phage cocoon while you're here, right? Someone will pay a nice helping of credits for it. ->runningdark
        **See what's up with the actual battle ->->battle->->
        **Next mission! ->espionage4
    *Accept the offer.
    ~alter(companions, Gman)
    ->runningdark
            
            =runningdark
            Maneuver Adrasteia space with as little energy emission as possible. Don't wake the 'phages!
            *Ignominious death!
                {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
                -else:
                ->END
                }
            *Success!
            ~get(phagecocoon)
            Extra quest reward etc etc etc
                **{not gabrindies}
                    ...Gabrin's not working for Miss Terri's. Whoops! And he's stabbed you in the back. Again.
                        ***kill that fucker!
                            ~alter(worldstates, gabrindies)
                            ****Well done! Next mission! ->espionage4
                            ****Check on the battle? ->battlestate3
                        ***{charm >= 5} Sweet talk him into realizing that you can still split the profits and it'll be far less trouble for everyone. Plus, you'll be able to get more credits from your own connections than he will.
                            ~drop(phagecocoon)
                            ~alter(credits, 1000)
                                {baldfacedliar >= 9}
                                    You've successfully convinced your handlers that you completed the mission as specified. Fortunately, no one in Elysium ever knows what's going on anyway.           ****Well done! Next mission! ->espionage4
                                                 ****Check on the battle? ->battlestate3
                            {shuffle:
                                
                                    -Well, you've got huuuge...stacks of credits and a black mark by your name in the espionage community. Not a bad trade off, all things considered.
                                    ~alter(pathblocked, pathblocked.spy)
                                    ~alter(path, -path.spy)
                                    Faction reputation issues, etc.
                                    ****Back to the battle?
                                    ->battlestate3
                                
                                    -You've got a poker face like a metal rod, and a patented expression of trustworthy innocence. Nice going. You've convinced your handlers you've been playing nice, anyway.
                                        ~alter(baldfacedliar, 1)
                                                 ****Well done! Next mission! ->espionage4
                                                  ****Check on the battle? ->battlestate3
                            }
                        ***{fuckingwithpeople >=5 || ingenuity >= 7} Two can play at that game. And you <i>do</i> still have the 'phage cocoon. Tell Gabrin that fine, you'll give it to him, you don't have to kill each other over this, and jettison it in his direction...with a timed flare attached, and get the hell out of dodge before the 'phages come.
                            ~drop(phagecocoon)
                            ~alter(worldstates,gabrindies)
                            ****Well done! Next mission! ->espionage4
                            ****Check on the battle? ->battlestate3
    =espionage4
    ~alter(worldstates, -lucentswitcheroo)
    Your final mission of the day has arrived. Congrats! In the course of a few hours, you've established yourself as the go-to girl for skullduggery and intrigue. Unfortunately, with great power comes great expectations. It is your task now to head back to the battle and succesfully extract a Legerdomaine? asset from the Zhestokost boss battleship, before your own side manages to destroy it. Oh, and that asset? Turns out they won't leave without their new boyfriend. Who's the friendly shipboard representative of the Zhestokost secret police, Vakhtang Moche. Who you've met before, an encounter that ended in a shocking number of explosions, six hundred dead albaxian jeweled parrots, and the tragic loss of at least one of your flawless eyebrows. Also there may have been some attempted murder involved.
        But, as your asset informs you, he's <i>changed<i>! He said so!
            *Well, true love is never wrong, right? Agree to extract them both--as long as you get some collateral, of course. You're not stupid, you're <i>idealistic</i>. 
                //contemplate collateral and difference between this and "just doing my job" option
                ->extraction
            *Yeah, no. As far as you're concerned, anyone daft enough to fall for Vakhtang Moche is probably more valuable dead than alive. But you can't risk him spilling plans and secrets to Zhestokost, not when the current battle plan to destroy the ship relies solely on narrative theory.
                    **Which means you better blow up that ship yourself, and fast.
                        ***Get thee over there!
                    ->battlestate3.bossbattle
            *Okay, well, Vakhtang is a murdering jackass, but it's not your job to make moral decisions. Apparently this asset is extremely valuable, and you've been told extreme measures are authorized to get him out. So do your job, and make sure any blowback hits the people above your head instead of you.->extraction

        -(extraction)
            Eh, figure out how this works in game later. 
                *Vakhtang, true to form, tries to kill you.
                    -(top)
                    **Kill him back! And...now his boyfriend is trying to kill you too. Great.
                        ***{questitems ? towel}
                            Fortunately, you are always prepared. Just tie him up and get on with it.
                            ->espionage4success
                        ***{ingenuity >= 3}
                            Well, this is inconvenient. Still, it's <i>your</i> ship. Just shove that fucker in the airlock and deliver him to your bosses.
                            ->espionage4success
                        ***Well, it was a stupid mission anyway. You're not supposed to be keeping people <i>alive</i>, for credit's sake. And space is a dangerous place, after all. How could you possibly predict that the asset would have just...forgotten to seal his helmet properly during extraction? 
                        ->epsionage4fail
                    **Talk him down. You did just save him from getting blown up, after all.  
                        {charm >= 9:
                            You're a fast talker, which is good, because you've got about five seconds to make your point, before you get a far less agreeable one to the heart. 
                                It takes you three. 
                                ->espionage4success
                        -else:
                            Well, that went about as well as expected. Try a different tack, and fast. 
                            ->top
                        }
                    **When faced with the imminent possibility of a laser to the internal organs by a vengeful secret politsya, there's only one solution: yonk his chain like the terminally flippant fuckhead you are and hope the momentum screws up his shot. So thank his boyfriend on luring the enemy into your nonexistent trap, giggle like a lunatic, then turn around and congratulate Vahkang on his excellent acting abilities in convincing said boyfriend that it wasn't all part of the grand Zhestokost plan, which of course you and he cooked up together, and watch "true love" go up in a beautiful conflagration of accusation and denial. 
                        {fuckingwithpeople >= 9}
                            It works, of course. Catastrophe and chaos go together like tea and biscuits, and your parents must have had a little of the oracle in them. Once you're done enjoying the train wreck, go ahead and shoot Vehktang in the head, and use your incredible powers of bullshit to get the asset back on your side and safely back to your handlers.
                             ~alter(fuckingwithpeople, 1)
                            ->espionage4success
                         {shuffle: 
                                -To your eternal surprise, this works. Nice.
                                ~alter(fuckingwithpeople, 3)
                                ->espionage4success
                                
                                -This...does not work, and now you have two people determined to kill you. Oh well. You're a pretty good shot yourself.
                                ->epsionage4fail
                                
                                -This fails, catastrophically, in the sense that you get shot by two very angry people instead of one. 
                                            {CnNjumpin < 3:
                                                     -*save yourself!
                                                        ~alter(CnNjumpin, 1)
                                                        ->top
                                                -else:
                                                        ->END
                                            }
                        }
       
       
        -(espionage4success)
        ~alter(worldstates, spycomplete)
        Oh, you're good at this. Accept your hard earned rewards, and the added benefit of not having to go on a suicide mission. Not for long, of course, because it's about time for that star over there to go supernova. ->NOVATIMEEE
          
        -(epsionage4fail)
        {not battlestate3.bossbattle}
            That...did not go well. Return shamefacedly to your handlers, and get sent into the battle for your sins. And by battle, we mean "fatally improbably long shot at a 'Weak Spot' that may or may not actually exist". 
            ->battlestate3.bossbattle.forlornhope
        
===battle===
{path ? path.battleleader}
WHOOO! It's fight time! And if you aren't too busy committing bloody murder or being an international cat of mystery, it's time to engage with the enemy.
 
    *Use your powers for evil--and profit! 
        ~alter(worldstates, totalfuckhead)
        ->traitor
    * {path ? path.guard} You've already got a job! Protect Katria! 
    ->escortmission
    *Use your piloting skills and aresenal of highly destructive weaponry! 
    ->tankitup
    *{path.spy} This is booooring. Go back to your spy business.->->



  
   

   
    =escortmission
   //assassin faction reputation tanked, yada yada, death squad puts a price on your head, blah blah blah
    You can only assume your erstwhile employers haven't given up on their intentions to eliminate Katria. Your job now is to outthink, outflank, and then take out your replacement, and not for a nice dinner on the ice moons of Erzatz 5.
    *You failed, and now your friend is dead and a whole syndicate of very murdery people are out for your head.
    ->->misc.deathsquad->->
        ~alter(worldstates, katriadies)
        ->battlestate3
    *You did it! What now?
        **Charge back into battle!
        ->battlestate3
        **{path? path.spy} {not spycomplete}You got some espionage to do!
            {espionage.espionage3:
                Super secret spy biz!->espionage.espionage4
                    
            -else: 
                Spy spy spy ->espionage.espionage3
            }

   
    =tankitup
    no prerequisites! Hooray.
    ~alter(path, path.battleleader)
    *{path ? path.flygirl && questitems ? plasmagun && charm >= 3} 
    You've got the charisma, you've got the skills, and you've got the highly unstable black market weaponry! Rally the troops and lead them to victory...for the moment. 
        **continue the fight!->battlestate3
    *{path ? path.flygirl && questitems ? plasmagun}
        You've got the skills, you've got the weaponry...you don't have the charisma, but that's okay. Other people just get in your way anyhow.
            ~alter(worldstates, shipdamaged)
            ->battlestate3.shiprepairs
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
    ~alter(worldstates, lucentswitcheroo)
   -(superspy) 
   {worldstates ? infointime}
    Fortunately, it's only Lucent you're dealing with, because thanks to your super spy shenanigans, the Finch reinforcements have been taken out of play.
   -(spyfail) 
   {worldstates ? reinforcementknowledge && not infointime && not treacherymostfoul}
    There's also the matter of the incoming Finch reinforcements, who, thanks to your poor decision making, are already on the scene and have taken out most of your squadron. Which means you've got a whole heaping helping of trouble on your hands. At least you're forewarned?

    -(nonespy)
    {path ? path.spy}
    Surprise! Finch is joining the party, and they brought snacks. By which, of course, we mean fresh troops and a lot of high tech weaponry. Yeah, whoever they sent instead of you kinda fucked up big time.
 *Charge into the fray, in classic ARPG fashion. I guess we should do that at some point, right?->bossbattle
 *{path ? path.hacker && companions ? ReenaJailbroken}
            {battlestate3.spyfail || battlestate3.nonespy}
            The notable thing about Finch ships, beside the a e s t h e t i c, is the super advanced, super breakage-prone tech. More moving parts, more failure points, etc. Fortunately, Reena knows this too, and on the way back from your failed mission, she's been working on a solution to take them out. Pitch in with your own expertise, and deploy a virus right into the Finch mainframe. ->bossbattle
            {superspy}
            Since you don't have to worry about Finch, you have the leisure to crash the Lucent streaming party! Hijack the broadcast and the limelight. What will this accomplish? Fuck if I know. Is this part of the rebel path? Might be. Might be.->bossbattle
 *{science >= 5 && questitems ? ctd} {battlestate3.spyfail || battlestate3.nonespy} Use your wits and your non-AI copilot to take out more than your fair share of ships. ->gravityslingshot
*{worldstates ? squidpropulsionsystem}
        You've got the element of suprise on your side. Charge into battle, immune to any EMP grenades, and wreak havoc.->bossbattle 
*   {path? path.spy} {not spycomplete}You got some espionage to do!
            {espionage.espionage3:
                Super secret spy biz!->espionage.espionage4
                    
            -else: 
                Spy spy spy ->espionage.espionage3
    }
* {path ? path.assassin}{not pathblocked.assassin}{not assassincomplete} You still have people to kill! Get to it!
        {assassination.Assassin3:
            Go murder, you murderer. ->assassination.Assassin4
            
        -else:
            Kill! Kill! Kill! ->assassination.Assassin3
        }
    
        =shiprepairs
            Unfortunately, you took damage. Lots of it! Your thrusters and weaponry are out of commission. Now what?
            *{path ? path.flygirl && pseudospacefuckery >= 3 && questitems ? towel}
                Some people might attempt repairs, some people might give up, some people might look to their friends for help, but you...you know the only real course of action is to whistle up a space squid and lasso that fucker to pull your ship back into battle like unto a celtic warrior queen of old. Good thing you brought your towel!
                ~alter(worldstates, squidpropulsionsystem)
                Squid propulsion activated! 
                **CHAAAAARGE
                ->battlestate3
            *{science >= 7}
                Your ship may be reduced to a heaping hunk of space junk, but not your brain. Use those engineering skills to fix up your ship and get right back to it. 
                ~alter(worldstates, -shipdamaged)
                **Once more into the breach, my friends!
                ->battlestate3
            *{ingenuity >=5 && questitems ? toffee}
            Well, you're no mechanic, but you've got brains, a copy of the ship schematics, and a pocket full of extremely sticky candy. This shouldn't be too hard, right?
            ~alter(worldstates, -shipdamaged)
            ~drop(toffee)
            **Well, you're covered in toffee, your hand is stuck to your nose, but the ship's fixed. In other words, you've never been more ready for a fight.
            ->battlestate3
            *{not abbydies}
            You're no mechanic, but you don't need to be, because your best wingbuddy is the queen of the machine shop. Radio for assistance!
             ~alter(worldstates, -shipdamaged)
             **You can always count on ya girl. Ship fixed, go back into the fray.
             ->battlestate3
            *Some may be discouraged in a situation like this. Not you! Unassailable odds won't stop you, because words like "unassailable" are way beyond your reading ability. So take a look at the damage and give it your best shot. Or not. It'll end up the same either way. An excrutiating death! ->END
  =gravityslingshot
    Okay, you can do this. You just need someone to get the {bossbattle: Zhestokost|Finch} boss ship into the target zone.
    *{battlecrewdead && abbydies && katriadies}
        Well, all your friends are dead, so you'll have to get to the launch spot, jettison CTD, fly back, and lure the ship into the target zone before it passes through. Easy peasy.
        ~drop(ctd)
        **failure!
        {CnNjumpin < 3:
                -*save yourself!
                ~alter(CnNjumpin, 1)
                ->->
            -else:
                ->END
            }
        **Victory!
        {bossbattle:
            ->NOVATIMEEE
            
        - else:
            ->bossbattle
        }
            
    *{companions ? Abby || companions ? katria}
        Fortunately, you have help. Get your human buddy to play bait, and go launch your inanimate one! Bonus points for a c-c-c-COMBO SHOT!
        ~drop(ctd)
        **failure!
            {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
            -else:
                ->END
            }
        **Victory!
        {bossbattle:
            ->NOVATIMEEE
            
        - else:
            ->bossbattle
        }
    
=bossbattle
FIGHT! FIGHT! FIGHT! And...Lucent's back on your side? Maybe?
~alter(worldstates, -lucentswitcheroo)
{not path.rebel} And, wait...you're getting a transmission from SS Chocolot, telling you about the weak spot. Ah, narrative inevitability.

*{science >= 5 && questitems ? ctd} Use your wits and your non-AI copilot to take out the Big Bad. ->gravityslingshot
*No fancy business, we're doing this the old-fashioned way. You hit anything enough, it'll blow up eventually, right? CHAAAARGE!
    **u ded.
             {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
            -else:
                ->END
            }
    ** Somehow, you pulled off a miracle! Buuuut...->NOVATIMEEE
*{not path.rebel} Well, there's no harm in trying, right? And you <i>are</i> the protagonist, after all. Go for it!
        **die spectacularly!
            {CnNjumpin < 3:
                -*save yourself!
                ~alter(CnNjumpin, 1)
                ->->
            -else:
                ->END
            }
    **{questitems ? coolbomb} Fortunately, you still have that shiny new antimatter bomb your anti-capitalist friend gave you. This seems like a good time to deploy it.
    **You might be doomed!
        --(forlornhope)
        Ok, well, the "weak spot" plan might be the stupidest thing you've ever heard, but then again, no one has ever accused you of being sensible. Anyway, you've never backed down from a challenge in your life.
                            {path.flygirl}
                            Lucky for you and Team Espionage Team, you've got some sweet moves, direct from the Crash Test Dummies. Do a barrel roll!
                                //ok, look, I have some theories as to how this would work in gameplay. Don't @ me.
                                It's a success, and you're a fucking legend. Unfortunately, you don't have long to bask in the glory. ->NOVATIMEEE

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
                                ***Next mission!->assasination2
                                ***Let's see what else is going on. ->pathtobattle
                    -else:
                    u done fucked up! self destruct sequence activated.
                {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
                 -else:
                ->END
                     }
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
                                *****Next mission!->assasination2
                                 *****Let's see what else is going on. ->pathtobattle
                **Genetically engineered viruses are your jam! And you can always count on them to get you out of one. This one is highly infectious, is grafted onto a tardigrade and can survive vacuum conditions, and kills in half an hour. Implausible? Sure. But then, so are you.  
                    Now all you need to do is get close enough to deploy your new monocellular best friend into the air vents of your target ship.
                    CUE MINIGAME WHERE YOU DO A FLYBY AND DROP THE VIRUS INTO SOME SUSPICIOUSLY CONVENIENT OPENING
                        ***You did it! Good job, you bioterrorist, you.
                        ~alter(stealth, 1)
                        ~alter(science, 1)
                         ****Next mission!->assasination2
                        ****Let's see what else is going on. ->pathtobattle
                        ***FAILURE. Whoops.
                            ~alter(pathblocked, pathblocked.assassin)
                            ->pathtobattle
//Maybe we give the player a couple of points to allot at the beginning? Just to make the skill checks more checky, if you follow.
    *{science >= 3 && path.hacker}
        This seems like the opportune time to deploy your computer skills. Write or find a virus, and then get close enough, undetected, to your target to upload your code onto their servers and kill their power. For bonus points, make it look like someone on board just accidentally bumped the big red button and set off a shut-down sequence.
            **Mission accomplished!
            ~alter(stealth, 1)
            ~alter(computerstuff, 1)
                ***Next mission!->assasination2
                ***Let's see what else is going on. ->pathtobattle
            **UPLOAD FAILURE, though undetected 
             ~alter(pathblocked, pathblocked.assassin)
            ->pathtobattle
            **You were spotted! And unlike the Etricarian shifting leopard, you can't change them. Whoops!
              ~alter(pathblocked, pathblocked.assassin)
            ~alter(reputation, -9)
            ->->misc.AUallupinhere->->
            ->pathtobattle
            
    *{path.diplomat} and charm intimidation or etc at a reasonable level
    Shady backroom deals and political skullduggery? You're there. Use your connections to machiavelli whoever you hate most into committing the murder for you.
        **Success! 
        ~alter(fuckingwithpeople, 1)
        ~alter(charm, 1)
                        ***Next mission!->assasination2
                         ***Let's see what else is going on. ->pathtobattle    
        **You got outmaneuvered. Uh-oh.
            ~alter(pathblocked, pathblocked.assassin)
            ->->misc.AUallupinhere->->
            ~alter(reputation, -9)
            ->pathtobattle
    *{ingenuity >= 3 && questitems ? coolbomb}
        You're pro fuckoff-awesome anti-matter bombs. You're anti notoriety. Antithetical? Absolutely not. Just use that handy dandy explosive your terrorist friend gave you and leave some flashing neon arrows towards whoever you hate most.
            **You're not as clever as you think you are. 
            ~drop(coolbomb)
            ~alter(pathblocked, pathblocked.assassin)
            ->->misc.AUallupinhere->->
            ~alter(reputation, -9)
            ->pathtobattle
            **You might be almost as clever as you think. Nice going.
            ~drop(coolbomb)
            ~alter(ingenuity, 1)
                ***Next mission!->assasination2
                ***Let's see what else is going on. ->pathtobattle
    *{pseudospacefuckery >= 5 && ingenuity >=3 && fuckingwithpeople >=3}
        You <i>could</i> kill someone the sneaky way, or the high-tech way, or the old-fashioned bloodbath way...but that's <i>boring</i>. {questitems ? towel} Luckily, you brought your towel. You don't need it for your plan, but it's nice to be prepared.
        Hail your target ship, insult the captain until they attack, and lead them on a merry pseudospace chase into candyland where your connection to all things uncanny and improbable lets you whistle up a taffy creature into body slamming your pursuit.
            ~alter(pseudospacefuckery, 1)
            ~alter(fuckingwithpeople, 1)
            ~alter(ingenuity, 1)
                ***Next mission!->assasination2
                ***Let's see what else is going on. ->pathtobattle
    *{stealth >= 3}
    Space dogfights are fun and all, but stealth is your pianissimo, you put the silent 'b' in subtlety, you're wearing custom InCognito noise-damper boots and three layers of aliases, you--well, you're much too sneaky to be going around boasting about it. Instead, don your cape of invisibility, commit bloody murder, and saunter away without a spot of gore on you.
        **Someone saw you! Turns out it's harder than you thought to fly under the radar in four dimensions.
              ~alter(pathblocked, pathblocked.assassin)
            ~alter(worldstates, AUinvolved)
            ~alter(reputation, -9)
            ->pathtobattle
        **Success! Nice.
            ~alter(stealth, 3)
                ***Next mission!->assasination2
                ***Let's see what else is going on. ->pathtobattle
    *You're not really good at anything, but you're willing to make an effort. Go in guns blazing, and fail spectacularly.
        **u ded
        {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
            -else:
                ->END
            }
        **You did it! And you also tanked intercorp relations for the next decade. Nice.
            ~alter(pathblocked, pathblocked.assassin)
            ->->misc.AUallupinhere->->
            ~alter(reputation, -9)
            ->->misc.reputationtanked->->
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
                ***More assassin business!->Assassin3
                ***You should probably get back and see what's going on with the battle. ->battle
            
    =Assassin3
    You're killer at this. Go get your reward, and your next contract. On...Vincente Verdegris?
    Serves him right for playing Double-Cross Simulator on company time.
    He's broadcasting from a station on the edge of Lucent territory. Go forth, and execute.
            *He ded.
                **Next! ->Assassin4
                **Meh. Got other stuff to do. ->battlestate3
            *U ded!
                            {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
                 -else:
                ->END
                     }


    =Assassin4      
    Look at you go, you coldblooded murderer, you. Go collect your reward, and your next contract. And today's lucky victim is...actually, you don't know who your next victim is, because no one else does either. That's right, you've been sent after Zeitgeist, the legendary and enigmatic pirate Whistleblower, digger up of dirt, accompanying skeletons, and all their very grimy, rotted laundry. Word on the space highway has it that they're planning to crash the digital war room of the mostly pseudospace side, and publish the intercorp incompetence in their illegal spacecast. And in the words of the ancient sage Meem, nobody liked that. Your task is to backtrace the hack, hunt them down, and execute them in cold blood--for the greater good (and even greater credits). Succeeding in this contract will make you <i>very</i> popular with executives and capitalist pigs everywhere.
                *{path.rebel}{worldstates ? takeupthemantle} As it happens, you know exactly where Zeitgeist is...well, you've got a pretty good idea. Who can really say they know where they are, after all? All you can say for sure is that they're currently located somewhere in the general vicinity of the captain's chair of your ship. And as much as you like murder, you like having your internal organs in their proper places more. And anyway, you'd never get the brains out of your chair. So what now?
                *{path.spy} plus faction rep yada yada yada. Grab your flashlight, put on your skulking shoes, and go talk to your contacts in the shadowy underworld of Elysium. 
                        **Information obtained!
                            ->zeitgeistfound
    
          
           -(zeitgeistfound)
            Well, after kicking up stones from one end of the sector to the other, you've found your target.
                 *{ReenaJailbroken}Reena does <i>not</i> approve of you killing her favourite spouter of righteously inflammatory rhetoric. And she has control of your life support systems, so...probably best to go find something else to do. Like take out that fuckoff huge Zhestokost ship that just showed!
                    ~alter(pathblocked, pathblocked.assassin)
                    ~alter(path, -path.assassin)
                    ->battlestate3.bossbattle
                *Take him down, and return triumphant. You're the belle of the red death masque. Accept your accolades--and a nice influx of credits--and head back to the battle. 
                     ~alter(worldstates, assassincomplete)
                     Buuuut wait! It's NOVA TIME! ->NOVATIMEEE
                *Actually, Zeitgeist kind of has a point. And you've always wanted an excuse to fuck over your employers. Join the pirates!
                    {not ReenaJailbroken} Unfortunately, your corporate overlords are well represented, in your company issued ship AI. And she's just turned off your life support.
                        {CnNjumpin < 3:
                         -save yourself!
                        ~alter(CnNjumpin, 1) 
                        ->zeitgeistfound
                        -else:
                        ->END
                     }
                    {questitems ? finchspyware} Well, you can do what you want, but the capitalist pigs are always listening, and that spyware has a self destruct button. Whoops.
                                {CnNjumpin < 3:
                             -save yourself!
                             ~alter(CnNjumpin, 1) 
                                ->zeitgeistfound
                             -else:
                         ->END
                     }
                     And you get to skip the "can we trust this fucker" quest! O happy day. ->witharebelyell.partycrasher
                
 //add turn counter to limit the number of options in one playthrough               
===traitor===
Go offer your services to the enemy. Should they accept, get your first task: convince your previous employers that the Finch reinforcements on their way are actually on their side.
//add companion alterations

         {not abbydies && companions ? Abby}
        Whatever you think about it, you're got your mercilessly practical, completely amoral best friend on hand to offer sage advice. And she's right, of course. And way more convincing than you are.->successfultreachery
        {baldfacedliar >= 5:
            They believe you, of course. You're better at lying than a consumption-ridden corpse, and you smell better.->successfultreachery
            -else:
        {shuffle:
            -They...believe you? Wow. You've got a better poker face than you thought.
            ~alter(baldfacedliar, 1)
            
            -You're not very convincing. Oh well. You were dead anyway. 
            {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
                 -else:
                ->END
                     }
            }
        }

        =spypath
        Right. Well. This is awkward. And so not your style. But neither is dying horribly, so...

    *Go back to your bosses and tell them that Finch is actually about to switch to <i>your</i> side. The reinforcements are there to help.
         {not abbydies && companions ? Abby}
        Whatever you think about it, you're got your mercilessly practical, completely amoral best friend on hand to offer sage advice. And she's right, of course. And way more convincing than you are.->successfultreachery
        {baldfacedliar >= 5:
            They believe you, of course. You're better at lying than a consumption-ridden corpse, and you smell better.->successfultreachery
            -else:
        {shuffle:
            -They...believe you? Wow. You've got a better poker face than you thought.
            ~alter(baldfacedliar, 1)
            
            -You're not very convincing. Oh well. You were dead anyway. 
            {CnNjumpin < 3:
                -*save yourself!
                    ~alter(CnNjumpin, 1) 
                    ->->
                 -else:
                ->END
                     }
            }
        }


-(successfultreachery)
You've convinced your erstwhile employers that Finch is on their side. Now what?

    *Now seems like a good time to bail, before people start asking inconvenient questions and bringing out the lawyers. You've heard the frontier planets are nice this time of year.
        ~alter(path, path.deserter)
        ->->misc.desertion
        ->traitor.successfultreachery
    *{traitor.spypath}Return to Finch, and offer your continued services as one shady motherfucker. You're in too deep to come clean now anyway.->treachery2
    *{worldstates ? totalfuckhead}Double the agent, double the agency. New orders await!->treachery2willing
        
        =treachery2
        Finch knows a good deal when they see one, and accept your services. But they're installing oversight software on your ship, because nobody trusts a traitor.
            {companions ? ReenaJailbroken:
                Which is fine, of course, because Reena, free from her corporate shackles, is more than happy to feed them lies.
             -else:
                And your preprogrammed corporate copyright ship AI is always happy to narc on you. Welcome to Elysium.
                ~get(finchspyware)
            }
            Now get your new orders, get back to the battle, and start sowing the seeds of discord in the ranks.
            Intercept your previous side's battle orders and take a little creative license with the contents.
            *Success!
                ->treachery3
            *Failure!
            ->treacheryCAUGHT

        =treachery2willing
        It's chaos time! Intercept your previous side's battle orders and take a little creative license with the contents.
            *Success!
                ->treachery3
            *Failure!
            ->treacheryCAUGHT
            
       =treachery3
       There's some stuff about a weak spot in the zhestokost ship, and your bosses aren't having it. Get back into the fray, with your "friends", and make absolutely sure no one makes that one in a million shot.
                *Saved!
                Zhestokost is about to win this battle, and things are looking good for you. But...there's that little problem of the star that's right about to go supernova. ->NOVATIMEEE
                *Failure!
                Well, the ship was destroyed. {treacheryCAUGHT:It may even be that you did it. "Accidentally".} Doesn't matter, of course, because it's nova time. ->NOVATIMEEE 
                
    =treacheryCAUGHT
   Either you have a tell, or someone narced on you. Fucking hell. Now you're persona non grata with literally everyone.
    Or are you?
    Ah yiss. It's triple agent time. Get to that boss battle and blow up the Zhestokost ship for your old-new employers.
    ->treachery3
        
===witharebelyell===
Reena, of course, is right. This <i>is</i> ridiculous, and you've always had an anti-authoritarian streak. It's why you're so good at what you do. Reena also has a Plan. It would be nice if she'd tell you what it is, but...
->spacepiracy

//IS JAILBROKEN REENA NIBU/DECI IN DISGUISE??? As in, did the (theoretically impossible in copyrighted Elysium) jailbreaking process let your future bestie slide in and take over? Of course it did, because no one's personality shifts that drastically just because you removed a few metaphorical locks. And why does Deci, having gotten a foothold in the past/this 'verse immediately decide to cause so much chaos, and make Cat persona non grata with Corporate Elysium? Tune in next level to find out!

    =spacepiracy
    First things first, last things in the middle, and middle things right back at the start. Get thee back to the Jet market, and find a pirate. Or, rather, follow Reena's somewhat obscure directions to pick out the right person.
    //wrong person choice here, add later
     Having done so, and offered your assistance to the cause, your new friend gives you a task to prove your trustworthiness. The battle is on, and one of the corps is using the chaos to transport some very important data across a few zones without pinging alarms. (Using the QuEEN network instead of manual delivery, of course, opens up the possibility of digital interception. It's a lot easier to hide a micro-micro chip than a message in Elysium, and the information contained on this is the location of a number of Miss Terri's Ultra Super Mega Top Secret weapons and drug development labs.) You wonder, if this information is so top secret, why the pirates know about it, and why they'd trust an untested asset with it. You don't ask, because you already know the answer: it's complete bullshit. Still, it's best to just get on with it, because you do want them to trust you, after all.
        Intercept and collect!
                *Messy is fun, but kind of obvious. Convince the messenger to hand over the data. You've got an MT contract, after all. (And that data? It's Chippy's daily spacecast. Priceless, of course, a treasure at zero credits, but not exactly high security.)
                *Shooty shooty gimme the booty
                *Mmm...no. You have better things to do than be jerked around by some paranoid wanker. Go back to the battle. ->battle
                 
            -(deliver)
                Nice. Everyone knows that you know that they know that you know the mission was bullshit, but we're all friends here now. And they've got a real request for you.
                ->partycrasher
                //option for kill or no kill etc
                
                
    
    =partycrasher
        It wasn't all bogus, as it happens. The data you stole? Also has the frequency of the super top secret pseudospace side corporate battle strategy meeting. Your job? Fly in close enough to hijack the frequency without the delay, and deliver the pirate manifesto/generally fuck shit up. Reena couldn't be more thrilled! You could, but only if it came with free drinks.
            ~alter(reputation, -15)
            You've made yourself very unpopular with the locals, though, and there's a death squad after you. You knew you were doing something right.
            ->->misc.deathsquad->->
            Plus, you've been so successful that Zeitgeist themselves wants to meet you. And you're starting to suspect that Reena is...not just a jailbroken ship AI. Oh well. You can talk about it later, because she's way too excited about meeting Zeitgeist to have a coherent conversation with right now.
                *This is cool and all, but you should probably check and see what's going on fightside. ->battlestate3
                *Let's goooooo already! 
                ->enterzeitgeist
    
    =enterzeitgeist
    Meeting with the firebrand is not as easy as your contact made it sound, because said firebrand apparently works from the Legerdomaine Labyrinth. This'll be fun!
    //I know we can't have too many different corp zones, but if I have to choose, this and adrasteia are the ones. Not least because it'll be great gameplay.
    Crazy flying, crazy backgrounds, nice, nice. Occurences occur. I'll throw in another quest here. The ultimate upshot of this is that the mantle is passed, and you are now the new Zeitgeist, solemly charged with pissing off the establishment and keeping the pirate legend alive. 
    ~alter(worldstates, takeupthemantle)
    *Back to business, I guess? I'll amend this route later to make more sense, but I should probably stop being unnecessarily complicated and just get this stupid outline done. ->battlestate3
    
===misc===
        =deathsquad
        Assassins are after you! Random encounter. ->->
        
        =AUallupinhere
        ~alter(worldstates, AUinvolved)
        Enter a third fleet.
        ~alter(fogofwar, 3)
        ->->
        
        =reputationtanked
        {reputation <= -9}
        Good job on becoming everyone's favourite target!
        {shuffle:
            - Uh oh. ->misc.randomencounter
            - Lucky duck. ->->
        }
        
        =randomencounter
        FIGHT! It's nice to be popular. ->->
        
        =desertion
        You keep <i>trying</i> to get out of dodge, but dodge keeps grabbing the ship controls and sending you back. Plus, there's the whole assassin death squad thing.
        ->->deathsquad->->
        Apparently, you're cursed by narrative imperative. Get thee back into the fray. ->->
        
===NOVATIMEEE===    
   // endings to be adjusted to final draft, obvs
    =outtahere
    And now...future you and some insane future AI who Reena assures you is like, super cool, seriously, are sending you messages telling you to get the hell out of dodge before something happens. It would be nice if they'd told you what, but apparently you're about to find out. Grab your friends and your ship, make like a tree, and leaf before the immediate vicinity gets swallowed up in a supernova.->END
    
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
    
   
   //PATHS AS YET NOT COMPLETED: traitor, rebel, anarchist, assassin, diplomat 
    
    //path interactions: flygirl + psychadelic + stickytoffee leads to squid lassoing; spy + assassin gets you a mad stealth booth and a reputation; hacker+rebel gets you the ability to crash the holomeeting, diplomat + spy gets you critical info and the chance to blow some shit up, deserter + psychadelic gets your hitchhiker friend sending you back to the battle over destiny or some shit, deserter + assassin adds an extra element of treachery when your fellow death squad members come after you, diplomat + assassin opens the option for strategic murder, flygirl + spy allows you to both save Abby and get back to base with your info in time, psychadelic+flygirl+rebel is the Ultimate Madness level, highly recommended, psychadelic+assassin lets you do the adrasteia realm mission in the most dramatic manner, anarchist + anything absolutely tanks your reputation and puts a bounty on your head, flygirl + battleleader + questitem plasma gun lets you lead an actually successful charge, diplomat + spy + psychadelic allows you to actually negotiate a peace--right before everything goes nova, and then get blamed for double crossing everyone. Scientist + flygirl lets you pull a fabulous gravity slingshot maneuver, and launch your crash test dummy strapped with an antimatter bomb riiiight at the weak spot of the ZK boss ship. Scientist + assassin means it's bioweapon time, assassin + hacker is a virus download. I will cull or add to these as I see fit. guard + assassin opens a doublecross path.
    
//the deserter path inevitably ends in either the collapse of the universe in a huge paradox or future!Cat and Nibu basically forcing you back to the battle. This will use up all your jump-ins. Either way, it does not put you in a good position going forward.
    
    
    
    
