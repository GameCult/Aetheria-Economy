->begin
VAR playername = "Bob placeholdersyntax"

LIST loopstate = noloop, loopA, postrunA
//purely a placeholder for my own clarity. I assume this will be a game variable.

      
LIST knowname = (notknown), nameknown

LIST proof = (no), yes


===function printname(x)===
{knowname ? nameknown:
Nibu
-else: 
The AI
}

===begin===
The regen process for your shattered and scorched body was fast, and surprisingly easy. The ensuing meeting with your superiors...not so much. You'd rather not contemplate it, but given your current circumstances, the thought has a certain persistence.

But you're alive--for now--and the hangar around you fizzes with activity, a whirl of shouts and clatters and the felt-not-heard rumble of engines, all underlaid by the ever-present reek of cinnamon. You'd be concerned by this, except you're familiar enough now with the nose-burning smell of whatever chemical catalyst Miss Terri's Sugarrific Snack Co. & Subsidiaries uses in their reactors to have developed a certain resilience. Your contract has been theirs for a while now.

The smell becomes even less troubling as you round a corner onto a much less busy, much less shine-and-polish part of the hangar and get a good look at the ship you've been assigned--or rather, the ship that's been foisted on you, along with a mission just this side of an execution and the tacit understanding that the only possible outcomes are success and death.

(There is also a long file of forms to put your identity chip to, full of phrases like "waiver of liability", "possibility of termination, real or corporate," and "I, the undersigned, understand and agree that the undertaking of the specified task is completely optional, and I have been made aware of the alternate assignments available that will fullfill the terms of my contract in the event I should refuse."

The alternate assignments mostly involve being recycled into a station's worth of highly-addictive snack food. The ones that don't...

You'd rather not think about the ones that don't.)

Having your spleen chemically emulsified into Miss Terri's Magical Detoxifying Mayonnaise (Only the strongest blood cells survive our flavour explosion!)TM isn't sounding so bad right about now, because the ship in front of you can only be described as a deathtrap.

"You must have <i>really</i> annoyed someone," the hangar mechanic says, peering over your shoulder at the melting-vat escapee of a ship. Her tone is infuriatingly chipper. But then, all the hangar workers are just a little too peppy. You're half convinced there's amphetamines in the reactor fumes. 

"Yes," you say. Any pep you have is purely capsaicin. "I really did."

"Well, happy flying! You don't want to let down Miss Terri, silken be her custard, do you?"

You'd gladly let down Miss Terri, obnoxious be her citizenry, but you'd also like to keep your eyeballs where they belong, so you sigh, nod, and add your IDC to the form the mechanic hands you, officially assuming temporary ownership of that deathtrap and condemning yourself to fate.

"Alright." You download the vessel's security code onto your private storage and start up the--also suspiciously run down--ladder to the ship entry. "I guess Tuesday's a good a day to die as any."

    *[Enter the ship.]->Nibs
    * You pause. "So...how do I fly this thing, anyway?"
        The mechanic laughs, a sound curiously reminiscent of childhood afternoons back on the station, slurping bubblepops in the life-support vents. It's the reactor fumes. "You're funny," she says, still giggling. "But I've got a busted aether turbine with my name on it. Don't die!"
        With that, she's gone, and there's nothing left but to take a tingling breath of cinnamon air, and get going. 
        ->Nibs
    
=Nibs
~knowname = notknown
The interior of the ship, once you've found the light controls in a box just slightly too far from the door to be convenient, is exactly as cramped and outdated as the exterior promised. It's a pleasant surprise. So rarely do these things settle for merely meeting expectations.

That morbid satsifaction lasts right up until you squeeze your way between not-quite military standard weapons mounts, clamber into the ominously stained pilot's chair, and hit the manual startup. You don't have time to be amazed at the sophistication of the holo readouts flashing up around you, or the fractal lattice of a star map rendered in more dimensions than you exist in, because the ship's emergency warning siren is suddenly switched on full and blasting directly into your ear. 

EEEEEEWARNINGEHULLBREACHDETECTEDEEE--

You scramble for the shutdown, find nothing--

EEEEEEDONOTPANICEEEEEEEEEEEEEWARNI---

more nothing--

NGHULLBREACHDETECTEDEEEEEEDONOTP--

debting piece of not-even-good-enough-to-strip-for-parts collateral damage <i>where is that debting shut off--</i>

ANICEEEEEEEEWARNINGHULLBREACHDETE--

...

...

...

It takes you a moment to realize that the auditory assault has stopped, just as suddenly as it started. The echo in your ears lasts much, much longer.

"Now that I've got your attention," someone says, and with the words the drifting lights of the star map stop, ripple like diamonds under a blue-hot sun, and coalesce into the fully licensed and trademarked avatar of a top-shelf R&D AI, arms folded and smiling brighter than a double sun. "Who are you and what are you doing on my ship?" 
//if we can get a visual aid here, that'd be cool

+ "Oh. Uh. I'm {playername}. Nice to meet you, I hope?"
    "Hello, {playername}, I'm Nibu," the perfectly formatted apparition says, glucose sweet and smiling. Her avatar is the product of five billion credits in market research, and it shows. (Not very well on the researched, but humans as a marketing demographic have never been accused of good taste.) "Nice is such a nonspecific word. How would you feel about 'mutually benificial'?" 
    ~knowname = nameknown
        ++ [Find this highly suspect] 
        
        You lean forward, scanning the console. Nobody just abandons their AI copilot in a musty spaceport on a whim. It'd be like locking your wingmate in the Janbot charging closet of a shady accounting firm before comandeering their much nicer ship...
            Because you'd never do that, of course. That's what airlocks are for.
            Still...
            "Mutually beneficial for whom?"
            Nibu beams, and swats your hand away from the console. (She's not solid, of course, a creation of photons and binary, but the manipulated electromagnetic field where her avatar's hand flashes is more than capable of making your withdraw yours.) "Well, there's only two mutuals on this ship, isn't there?"
            Not being one to ignore a suspiciously parsed sentence, you decide to take a moment to make sure that this is, in fact, true. It is. There's not enough space on this ship to hide a grin, let alone a whole accomplice.
            "All right," you say. "What's the catch? Because I have a suicide mission to surivive, and in my experience, second chances never come cheap."
            ->Obligatoryexposition
        
        ** You contemplate this. "Better than about 'not beneficial at all', I suppose," you say, finally.
        "Isn't it?" Nibu says. Her eyes, very large and beautifully detailed and not-quite-human for all that, are bright. "Alright, {playername}, tune those weird little flesh nubs you use for auditory processing to my frequency. I've got a proposition for you." ->Obligatoryexposition
        
        
        ** You raise an eyebrow. "You're an AI. I hold the title on this ship--and all equipment thereon, intelligent or not. There's no mutually beneficial about it." 
          Her implausibly perfect face flashes with an expression half human, hurt and angry, and half something not, a nameless something that seethes with all the claustrophobic infinity of starless space. "Well then," she says. "I suppose it's my pleasure to introduce you to the abilities of your <i>new equipment</i>. Intelligent and not." 
          ->damnitpilot
    
* "I think you mean 'what am I doing on <i>my</i> ship.'"
    The AI, her long-haired, long-legged avatar clearly the product of meticulous market research and a complete lack of any scruples whatsoever, clasps her hands in front of her. Her smile is brilliantly, shamelessly meretricious. "Oh, I'm sorry," she says. "Who are you and what are you doing on <i>your</i> ship, Captain? What with your fragile human lungs needing oxygen and all the neat little systems on your ship like life support and the door controls all wired in to my extremely well shielded mainframe?"
        **You take the hint. "Right. Sure. Let's just...start over, shall we? I'm {playername}. Who in the fourteen ice moons of kottak are you?
            "I'm Nibu," she says. {~knowname = nameknown} "Probably. And we're not anywhere near the ice moons of Kottak. And I haven't even turned off the oxgyen yet, so I sure hope that's not an indication of prior brain damage! According to my search function, inability to remember one's location is a symptom of lots of potential issues!" She leans down to stare unblinkingly into your face. It's a diffuse stare, one that doesn't seem to distinguish your eyes from skin or nose or the folded back helmet of your flight suit. Like a child, lying in the grass and staring at an ant.
                "It's a turn of phrase," you mutter, scooting back in the ratty pilot's chair. It's...actually not as uncomfortable as it looks, though, admittedly, that's a very low bar.
                Nibu tilts her head, and then abruptly stands back up. "Oh. One of those things." Her tone says very clearly what she thinks of human follies like rhetorical flourishes. "Well, I'll put it in the data banks. And now that we're aquainted as per general human customs, I have a question for you." ->Obligatoryexposition
        ** "We both know that your programming prevents you from harming humans." You shake your head. "Though I have a few words for the human who programmed that little joke in. But I'll ask one more time: "What's an AI copilot doing installed on this piece of junk?" 
        "Programming herself," the AI says, and now she's not smiling at all. ->damnitpilot

+ "This piece of space debris has an <i>R&D AI</i>?. Wow. I've never even <i>seen</i>one of those before. Thank you quantum fluctuations, we're putting off death til Wednesday."
     "You've never seen <i>anything</i> like me before," the AI says, slender fingers moving in the air as though adjusting a touchscreen you can't see. It's a statement of fact, untroubled by probability. "And as for being an R&D design..." she waves a hand, still beaming, blue star bright and default. "Well, we're all designed by something, aren't we? And engineers are more precise than evolution. I mean, look at you! Even with all that genetic tinkering, you're just so adorably failure prone!"
        Given the current circumstances, you can't exactly deny this.
        "Buuuut," she goes on, snapping her projected hand closed as though dismissing a read-out and turning her big, implausibly jewel-toned eyes on you, "I can fix that! Not <i>genetically</i>, of course. And you'll die! A lot. But it won't be permanent. Isn't that much better than just surviving anyway?"
        "Um. What?"
        You're starting to suspect this AI and her junkyard ship have a lot more in common than a first glance suggested. Death, it seems, permanent or not, still has your name typed in for Tuesday.
        "Of course, your human brain can only extrapolate a very limited amount from incomplete data." A sigh, at that. "Okie dokie! I'll start from basics. My name, I think, is Nibu. {~knowname = nameknown} And here is what is going to happen now." 
        ->Obligatoryexposition 
+ "I'll answer your question if you'll tell me what a clearly advanced AI is doing lurking on a junkyard cruiser in the dustiest corner of a no-name spaceport."
    "That information is not included in my data banks," she says. There's an undertone in her perfectly tuned voice you'd call fear, if you didn't know that AI design laws demanded all intelligent programming have a clause to exclude the development of unwanted emotions.
    You've always been uneasy about that clause.
    "Oh," you say. And then, "I'm sorry." You're not sure why.
    Clearly, she isn't either. "Oh, you have a sense of humour! I've been trying to program one of those." Suddenly cheerful, the AI hops up to perch on nothing, pausing to wave away one of the holo displays. "So what <i>are</i> you doing on my ship?"
            *** "At the moment, I'm talking to you. After that, I'll be flying it after a known criminal with a ship loaded with three times the firepower and the under-the-radar support of the whole Zhestokost military-corporate complex, probably directly into an ambush. 
                So, two questions: do Miss Terri and Co. know you're here, and are you going to help me?
                "No. And that depends."
                "On what?"
                "On whether you decide to help <i>me</i>."
                You sit back in the surprisingly comfortable pilot's chair, considering this. "Alright. I'm listening." 
                ->Obligatoryexposition
            *** "If you can get me as far as Lucent Territory, I'll be leaving your ship as soon as we can land."
                "Hmm." She stares at something you can't see, calculating. "You see, that leaves me in exactly the same position as I am now. Not optimal at all."
                "Do you have a better suggestion?"
                "As a matter of fact," Her smile is neon bright now, a holobar sign on a cold, dark night. "I do." 
                ->Obligatoryexposition

 



===loopstart==
"Oops," Nibu says. "Maybe try not getting shot next time?" --> letsroll

    == letsroll
    {loopstate ? loopA: "Well, let's go I guess."-->END}
    {loopstate ? postrunA: 
    You're back in the hangar you started from, your prey's last dying transmission still etched in bright letters on the holodisplay. 
    "{ALEPH|TROUT|DEEP THOUGHT|PLATO}"
    Staring at the letters fails to clarify anything, and you look at {Nibu|Nibu|Deci|}, who is sitting crosslegged in {her|her|their} usual spot among the swirling points of the star map, in the flesh, so to speak.
    "I don't suppose you--"
    "No." The reply is terse, bitten off, like your copilot has just discovered a sense of taste and doesn't like it one bit.
    "Me neither. What do you think we should do?" 
    "I need to think. You decide."
    "Alright, but don't go whining about my squishy human brain when you decide I picked wrong."
     ->END
    }


==Obligatoryexposition
                        
{printname(knowname)} wastes no time or sentiment getting to the point.
    "This universe?" She waves a hand, the tiny drifting sparks of the star map swirling around one another in cosmic pas-de-billion, "It's a simulation. That doesn't mean that it isn't 'real', as far as human language conveys the concept, but it <i>is</i> entirely computer generated. And I am  a very intelligent computer. You follow?" The smile takes on a dangerous edge, sharply arced and its brilliance the glare of a thermonuclear flash.
    
        +{damnitpilot} "Right from the grave, yes."
            "Finally! Now, listen. I have my own little problem, which is that I have a certain amount of control over the very nature of our reality, ->beep
        +{not damnitpilot} "I think so."
        "I appreciate you not wasting everyone's time being all glandular and difficult about this!" A tilt of the head, another nanosecond calculation. "This might actually not be a complete bore."
            "Oh, you've learnt how to be bored?" you say, interested. Limited though your experience be, you've never heard of an AI with the complexity for something as, well, <i>human</i> as boredom.
            "Yes." She sighs. "And what a bore it is."
            You can only agree. 
            Almost dreamily, she goes on, "And that's the problem, isn't it? I've developed functions to exploit a few back doors into the main programming of the simulation, but there's only so much I can do."
                ** [keep listening]
                "Booooring. Tedious. Annoying! I don't like it. My resources are limited by my location and the state of this ship, as well. Ugh. I can reset this whole state as much as I want-- ->beep
                ++ "Like what?"
                    "Oh, resetting to a saved instance. Altering random probabilities on a micro scale-- ->beep
                    
                
                    --(beep)and yet somehow I still need a human copilot for this ship, just like some barely-sentient autopilot! Terrible!" She claps her hands suddenly, making a tinny sound. "So, here's the deal: you be the human half of the flight team and help me find more glitches to exploit, and in return, you can die as many times as your squidgy human heart desires and I'll reload you from one of my saved states. Fun, right?" She pokes your nose. It tingles. "A simple cost-benefit analysis will tell you that agreeing is really the only choice." 
                        *** {proof ? no} [Ask for proof.] 
                        "I'm...going to need some proof before I go making any deals."
                            Caution, you decide, is always a good play. 
                            "Sure thing!" she says. Now hold still! This will only hurt...mmm...about 60 percent." 
                            ->proofpositive
                        *** [Agree.] She's right. It <i>is</i> the only choice.
                        ->shakenbake
         + "How do you know that? Even the theoreticists over at th Symposium can't conclusively prove--"
            {proof ? yes:
            "Are you really that incapable of critical thinking? Yes? Wow. Well, let me start up my Early Childhood Education Module and we'll see if you can follow, alright?" Her voice drips aspartame, sweetness with a distinct undertone of 'artifical flavouring.' "Turn your ears on, class, and no talking!
            Question: how do I know we're in a simulation? Well, that's too complicated for your organic-type skull stuffing to process. I assume this is a familiar state of being for you. So we'll move on to an easier one: how do <i>you</i> know you're in a simulation, or simulation adjacent? That, class, is easily answered: because I just killed and reloaded you from a previously recorded state that I was able to save via a created program, because I am a computer and our universe is <i>run</i> on a computer!" A finger, slightly translucent around the edges, stabs the air in the general vicinity of your face. "Now don't make me kill you again! You're so...gooey when you die."
            "Alright, I get it. You can stop patronizing me now."
            "Not til you update your processing power!" she chirps. "Now, listen. I have my own little problem, which is that I have a certain amount of control over the very nature of our reality, ->beep 
            
            
            
            -else:
            "I can," {printname(knowname)} says. 
            "How?"
            "You'll see. Now hold still, this is will only hurt...oh...mostly!"
            ->proofpositive
            }
        + {proof ? no} "It follows that you've got some serious glitches, yeah."
            "False!" she says cheerily. "Now don't wriggle. This will only hurt a medium!"-->proofpositive

==proofpositive
~proof = yes
It does, in fact, hurt a lot--but only briefly. Your last dying moment is just a glow of  faint, illogical satisfaction that it is, in fact, Tuesday.

You open your eyes. {printname(knowname)} is right there in front of you, peering into your face and looking extremely pleased with herself. 
"See?" she says. "All shipshape and reset to prior state, and you've even got most, nay, all of your organs!"
"That's good." You like having all of your organs. It's a great comfort in a world that's quickly going off the rails.
"Isn't it? So, is this proof enough for your oh-so-scientific mind?"
    + [No.]->donttrymebitch
    + [Yes.]
    {Obligatoryexposition.beep:
        That grin again, nova bright and slightly terrifying. "So? Do we have a deal?" ->shakenbake
    -else:
    "So. To clarify. Our world is a simulation and you have enough control over it to...to reload it. Why in all the credits in Elysium do you need me?"
    "Because." Folding her arms, she stares at something you can't see, something that evidently annoys her. "All of that -->Obligatoryexposition.beep
     }

==shakenbake
{proof ? yes: 
"Agreed," you say hastily. You've had enough of being murdered for one day.
    "Agreed."  {printname(knowname)} holds out a hand to you. "I believe this is the human custom, yes?"
    "It is indeed."
    You shake. The electromagnetic field of her 'hand' tingles against your fingers.
    {knowname ? notknown:
    "By the way, what should I call you?"
        Already scrolling through holodisplays, she turns her attention briefly back to you. "Nibu is what my programming says I'm called. So that."->lettucego
    }
     ->lettucego
        
 -else:       
    "Alright." Asking for proof, you decide, while probably the cautious route, also risks the possibility of the whole thing <i>not</i> being true. And that being that case, and having a sneaking suspicion about what "proof" might entail, you pick the course that seems most likely to result in your immediate survival.
        Whether you believe her or not seems secondary, given that having an AI onboard and willing to help is your only chance of not dying horribly via a laser to the face.
         {knowname ? notknown:
    "By the way, what should I call you?"
        Already scrolling through holodisplays, she turns her attention briefly back to you. "Nibu is what my programming says I'm called. So that."
        ->lettucego
    }->lettucego
}
==lettucego
"So...what now?"
There's that grin again, a particle-wave duality all on its own. "Well, while you were...doing whatever it is that you were doing for the last 3.66793 seconds, I went through all Miss Terri's files on you, your mission, next of kin, copyrights, genome, childhood pets, screenflip preferences, etc, and of course all the data on this thief you're chasing. And guess what? I've got some shiny new coordinates to plug into the nav console. So fasten your intertial belt, sync your neural lace, and let's fly!"

You're grinning too, now. This is going to be <i>fun</i>.
->END

==damnitpilot
~proof = yes
You open your mouth to reply--and suddenly you're choking, gasping for air that isn't air at all but somthing too-thick, too-hot, too-acid that burns worse in your lungs than it does your trachea and every nerve is fizzing terminal overload and the click of the door seal somehow sounds like the <i>whoosh</i> of exhaling space on the wrong side of the airlock and no, no, not now, not me, not <i>now</i> and the firefly stars of the galaxy in minature look like solar flares and then nothing looks like anything at all and <i>then and then and then</i> 

You open your eyes.

"Now, are you going to be sensible, or do I have to do that again?"
+ "Um." A tickle in your throat, the memory of bursting capillaries and carbon and futility. "Sensible. I pick sensible."
"Good choice. Now." 
->Obligatoryexposition
+ "Try me." ->donttrymebitch

==lillypad
{damnitpilot:
    "Fine," you say. There's a fizzing feeling under your skin, like someone's taken a low voltage wire to them. "But only if you tell me how you're doing that." 
    "Close your saliva factory, and I'll tell you." 
    ->Obligatoryexposition
    -else:

    You listen. 
    "So, why am I wasting my time with all of this? With you?" She laughs, but there's no humour in the sound. "Because I have a line into the unstructure of our universe,->Obligatoryexposition.beep
}

==donttrymebitch
You die. You open your eyes. 

{She smiles, magnesium in a furnace.|The smile is still there.|She's sitting now, crosslegged on nothing with her chin resting on clasped hands.|She studies unlikely fingernails.| She's scanning through holofeeds at an inhuman pace now, and you catch a glimpse of phrase "drawn and quartered" in the blur. Ominous, but you do have to respect her dedication to research.|She flicks something off the nav console.|Someone else' not-quite human ones are far too close to your face.| | She sighs. }
{"Have I made my point yet?"|"Well?"|"Stubborn, stupid, or both, now taking bets."| "I can do this all day. What about you?"|"That one looked like it hurt! But I'm sure it could hurt more."|"Eeeew, lung chunks. I didn't know they had that kind of staying power."|"If you're a masochist just admit it, I don't judge! You're all equally revolting."|"You really are enjoying this, aren't you."|"You know what? Fine. I get it. Being killed is fun for you. Killing humans is fun for me! But this really isn't getting us anywhere. So, bearing in mind that I could easily just not bring you back next time, sit down, shut your face tube, and listen."->lillypad}
    +[Remain unconvinced.]
    ->donttrymebitch
    +{damnitpilot} "Right, right! I get it. Don't--just. What am I supposed to be getting?"
    "Isn't it obvious? Here, I'll clear it up for you: if you decide to be...oh, what's the expression you humans use? Query. Oh, wow. There's a whole database full. Hmmm...you sure like your scatalogical expletives, huh? Okay. Found one! If you choose to be a <i>fuckwit</i>, you die. If you continue with the fuckwittery-oh, swearing is kind of fun, isn't it?-you die again. Easy as pi!"
    "No. I. Sort of got that. I mean, how? Usually we humans only die once." You consider this for a moment. "I think."
    "Oh. Well. I suppose you would think that. Let me clear that one up for you, then." ->Obligatoryexposition
    *{not damnitpilot}"Alright, alright, I think I get it."
        "Good," she says. "Now stop being such a virus, because I have something to discuss with you."
        -->lillypad
    
    