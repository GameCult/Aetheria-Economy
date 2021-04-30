
===outbuildings===
The outbuildings of what you can now clearly see to be a factory--and you were contracting with Miss Terri long enough to recognize their particular flair--are as empty as the rest of the landscape. The wind is even stronger here, and sharpened with an acrid tang as well as the ubiquitous stinging sediment. {lab_safety ? on: You're grateful for the slight protection of the safety goggles, lunch-scented as they may be.} Inside, then, seems the obvious choice, though undoubtedly that holds it own, uniquely terrible terrors.
-(choices)
*[look for an entryway] ->frontdoor
*{Inventory ? MashaGrenades} [Make your own entryway. With grenades!]->grenadier
*[search the area]->areasearch
+[return to base] ->explore

=frontdoor
It's not hard to find. The factory, as factories tend to, has a front door. You run through the obligatory pre-exploration of dread unknown check: sidearm, lucky charm, comm connection, gear to specifications. Everything seems alright, and you head in.->entryhall

=areasearch
You don't search the area yourself, of course. That's for people who don't have an overly curious AI peering over their metaphorical shoulder.
"Could you--" you begin.
"Alread have," Nibu chirps in your ear. "And guess what? There's a data cache stored on a signal drop juuuust...there. You have it. It looks like there was an error and it didn't send."
~pickup(codex2)
->codexthesecond

=grenadier
    This seems like a good time as any to try out Masha's "Home-ades." You open their box--noticing, for the first time, the lead lining--and take one out.
        *[throw the grenade at a likely wall]
            {shuffle: 
            -You have good aim!->kaboom 
               
                
            -Masha's home-ades are 49% unstable. This may be good odds in the lab, but not for you. You can hear Nibu cackling as your body vapourizes in a spray of superheated organ tissue.->END
            }
        *[decide against it]
        You're not that desperate yet, you decide. There's undoubtedly a safer way in.->choices
    -(kaboom)
    Or very good luck, because the blast radius goes...mostly in the right direction, and when the smoke eventually clears and you've got your hearing back, you see a dark hall in the poured carboncrete wall. Still choking slightly from the blast, you stagger forward and enter. 
    ~questtoggle(Bangers_and_Masha, complete)
    ->candyroom
    
===entryhall===
~questtoggle(Map_Magic, quarter)
The front door--a surprisingly filigreed affair of curling steel that's far more ominous than it has any right to be--opens easily at a push.->atria  

-(atria) You find yourself in a large atrium, containing several desks, a gargantuan fountain in the shape of a squid pouring what appears to be dark chocolate from its tentacles, an overturned jar labelled "Para-mels", and a dead body. 
    -(atriatop)  
    + [rifle through the desks]
        A hasty ransacking of the drawers turns up {~ a lot of suspiciously ashy lint->atriatop|nothing->atriatop|a packet of Meatiorites bacon-flavoured jellybeans ->beans|an individually wrapped piece of Oh Noogat-"A creamy, escape pod flavoured nougat in a deliciously chocolaty airlock"->sticky}
    +[examine the fountain]
        {It's a monstrosity, for sure. You feel gaucher for having looked at it.|The fountain is still there, and still hideous.|"Why do you keep going to stare at that thing?" demands Nibu's voice in your ear. You don't have an answer.|It's still there. You have the awful feeling the tentacles have shifted sometime while your back was turned.}->atriatop
    *[examine the jar]
        It's a small jar, empty now, but evidently not for long. You carefully waft the scent to your nose from the opening--lab safety!--and inhale. It's sharp, spicy, slightly acidic, like a lemondrop all dressed up in fashion combat boots and a bad attitude. Something about it raises the hairs on the back of your neck.
        You look closer at the label. It's a bit scorched for some reason, but under the large print "Para-mels", you can just about make out the word "experimental" and a half charred "psycholo-"
        Ominous indeed. ->atriatop
    *[examine the corpse]
        The body is dressed in recon gear, which is about all your can tell, as the entire face has been burned away. Still, a quick scan for an identity chip informs you that this is Operative Yanette Gulbthri, one of the 'tourists'. Shuddering, you turn away, pausing only to lay the murdered Yanette's dustscarf gently over her ruined face. ->atriatop 
    + [go through left door]->candyroom
    + [go through right door] ->offices
    
--(beans)
~trickortreat(meatiorites)
You glance around furtively, and slip the candy into your pocket. ->atriatop

--(sticky)
~trickortreat(ohnoogat)
The wrapper is very sticky. You put in in your pocket anyway. ->atriatop


===candyroom===
{halloween ? not_recieved: ->onecandy}
{halloween ? recieved && LIST_COUNT(CandyBag)>1  && LIST_COUNT(CandyBag)<= 12: ->twocandy}
{halloween ? recieved && LIST_COUNT(CandyBag) == full: ->threecandy}
=onecandy
~Map_Magic++ 
 For a moment, you think you're dreaming--or flying high on some really good cyberchems. The room you've entered, in contrast to the camouflaging greys of the outer walls, is a dizzying assault of poured resin in primary colours. It takes you a minute to focus on anything but the nausea inducing walls, and when you do it's to fizzes and pops and rumble of conveyor belts and machinery. It would be comforting, if not for the distinct lack of anything remotely living.
    ->glorb
  
=twocandy

The candy room seems even more eyewatering than before. 
"What did you bring me?" Glorb asks, a buzzing fairy light around your head.
You dump your haul in the spectrometer, and promise to return with more.
"Thanks!" Glorb chirps.->candyroom.candytop

=threecandy

When you return to the candy room, having successfully scoured the galaxy for every known iteration of Miss Terri's confenctions, the place is a bubble of noise and delicious smells. 
"Oh, thank you, thank you, you are truly an agent of Miss Terri, rosy be her waters," Glorp squeals. "That's the whole catalogue! You're a peach meringue."
[quest reward deposited.]->entryhall.atria


    =glorb
    "Welcome, human!" a high-pitched voice squeals. You practically collapse from surprise. "To the candy lab! You sure aren't supposed to be here, are you?"
    You wisely choose not to answer this.
    "It's alright," the voice goes on. "I'm not mad."
    "It's an AI," Nibu hisses. "Well, an A sort-of-I, anyway."
    You sort of gathered that from the "welcome, human," but there's no point in answering.
    "And you are?" you ask instead, to the AI actually in the room.
    "I am Glorb."
    This is not as enlightening as the AI seems to think. You wait.
    "I am the candymaker," Glorb goes on. "I make candy. All types! Chocolate and teeth and...and...-candy not found-. That's not how that's supposed to go. But one of those nasty Finch people creeping around wiped my recipe files."
    "Right," you say, edging towards the door on the off chance you might be also considered "one of those nasty Finch people". You are, illegally, working for them, after all.
    "But you are a true jellyhead! A devotee of our Miss Terri, flaky be her scones. I recognize the employee chip. That's why I spoke to you."
    "I may have edited your identification a little," Nibu whispers. "Don't get all human and panicky on me, according to your employee file you're in <i>excellent</i> standing. And in a friendly prank war with Tim from HR."
    "Oh, yes." Hasty affirmations seem called for here. "I love Miss Terri, floaty be her...islands."
    "I'm so glad! It's been so long since I have had the chance to commune with a fellow jellyhead," the AI sighs. "Your arrival is an acid rain of soothing FusionPepperMints on my circuits. I need you to do something for me."
    Of course.
    "Without my recipes, I can't make candy. And if the candymaker can't make candy, how can the candymaker be? The files are gone. But if you can bring me samples of the candy produced by Miss Terri, chewy be her mochi, I can reconstitute them. I have copy rights, of course."
        *"I suppose I could do that." ->candytime
        *"Sorry, accepting missions from strange AI's has not served me well so far."
            You're almost deafened by the sudden whine of ear-piercing feedback in your skull. Nibu has clearly succeeded in programming "pettiness", if nothing else.
            "Oh. Then you can die!" Glorb says, chipper as a chainsaw, and you find yourself quickly and unexpectedly dead. ->END
    -(candytime)
    "Wondrous!" Glorb chirps. "I'll just give you a list, shall I? Now go zoom off around the galaxy like a little bee with feet paddling in the pollon of the sugar plants--and bring me my candy!"
    ~questtoggle(halloween,recieved)
    [Quest: The Great Candy Caper recieved]
    
"You can look around, if you like." A tiny buzzing point of light, like the will'o'wisps of old Terran myth or chain lightning, unchained, materializes, bobbing around your head.
"Basic," Nibu mutters. "Can't even manage a silhouette."
You refrain from noting that Nibu has complained, frequently and loudly, about the fact humans expect her to appear in moderately anthropomorphic form.
    -(candytop)
    + [examine the walls]
        {lab_safety ? off: 
        You take a closer look at the walls, or try to. Your eyes can't seem to focus. The colours begin to curl, meld, merge, a dance in a spectrum you're not sure you're supposed to be able to see. When you finally return to reality, you find yourself back in the atrium by the chocolate fountain, bemused and unpleasantly dizzy. ->entryhall.atria
        }
        {lab_safety ? on:
        You take a closer look at the walls. Through the scratched and slightly sticky lenses of your safety goggles, the outline of a door begins to form. You press tentatively on the handle, but it appears to be locked.
        
            +[leave it] ->candytop
            *{Inventory ? key} [try the key you took from Catwin's body]->caskofam
        }
    + [examine the candy machines]
    At some point, you can tell, the toffee extruders, gumball rollers, and jellybean hydroponics churned out delicious and nutritious snacks to the exacting specifications of Miss Terri's Sugariffic Snack Co. Now, the maw of the toffee machine is gunked shut with something that smells like some unholy combination of burning rubber and strawberries, the gumball roller is jammed, having evidently been instructed to produce gumpyramids instead, and the jellybean hydroponics...well, there's a disconcerting whistling noise coming from the jellybean hydroponics, and you have enough experience not to venture anywhere near that. Still, you take the opportunity to scavenge for any appropriately flavoured candy that may have escaped the carnage. You come up with {~ absolutely nothing.|Miss Terri's Super Tough Toffee->toffee|Mint-alls Hi-Focus Drops.->mintally|Liquor-<i>ish</i> ->drunk|Marshmellows ->chillax}->candytop
    + [leave] ->entryhall.atria
=toffee
"Revenge is sweet. Miss Terri’s jawbreaker toffee is sweeter. Leftovers are excellent for ship repair!"
~trickortreat(toughtoffee) 
You slip the sweet in your pocket, and look around.->candytop
=mintally
~trickortreat(mintalls) 
You slip the sweet in your pocket, and look around.->candytop
=drunk
~trickortreat(liquorish)
-"Sort of candy, sort of alcoholic, all delicious!"
You slip the sweet in your pocket, and look around.->candytop
=chillax
 -"guaranteed freshly chilled."
~trickortreat(marshmellows) 
You slip the sweet in your pocket, and look around.->candytop

===offices===
You find yourself in what appears to be an office, the walls pastel purple and furniture that looks as though it came directly from bubbly, jewel-toned hell. Whose office it is, you don't know, but you never want to meet them. Another door leads from the far end of the room, painted green and distinctly ominous. You swear you can hear something whispering behind it.
    -(officetop)
    *[rifle through the desk]
        A search of the obnoxiously be-baubled drawers turns up {~nothing.->officetop|a packet of Beta Bits-"Fish food for humans!"->betab|a tube of SureBet Sherbet->nom|a packet of Mouth Adapting Gummy Molars,->teefs}
    *[examine the built in computer]
        A quick glance at the computer built into the flame red desk reveals one final message from the infiltration team. This time, there's no attempt at encryption.
        ->codexthethird
        --(back) 
        "Wow," you say, absentmindedly munching on the pastilles piled close to hand by the display. "They finally took the lead out, huh?" 
            ~questtoggle(tourist_bus, half)
            ->officetop
    *[look in the closet]
    There's another body on the floor of the small closet. This one has no eyes, and there begins to grow a strange, sickening suspicion of just where they went. 
    The ID chip identifies the curiously undecomposed corpse as Philobert Hobthan, infiltration specialist, copyright Finch Cybernetics. You look down at the pitiful shape, feeling suddenly guilty for your own immortality in the face of that dead, eyeless stare, then quickly bend down and pull his jacket over him.->officetop
    +[go through the other door]->vatroom
    +[return to atrium]->entryhall.atria
 
  -(betab)  
  ~trickortreat(betabits)
  , which you stash away in your pack. For later.->officetop 

-(nom)
~trickortreat(surebetsherbet)
"-each fizzy, sweet-tart purchase enters you automatically into an annual lottery held by Miss Terri’s. What’s the prize? Well, it wouldn’t be fun if we told you, would it?

legal note: by purchasing this candy you agree to 1 non negotiable entry into the annual Sugar Bowl Lottery. Winning entrants are required by law to appear at the Miss Terri’s main factory on BonBon 26 on a date to be specified by the MTSSC Legal team. No unauthorized absences will be tolerated." 
You squint at the tiny writing on the briliant yellow tube, and, shrugging, add it to the stash.->officetop

-(teefs)
bearing the image of a disturbingly anthropomorthing tooth and a speech bubble reading, "Ever thought “Gee, my teeth are so uppity, always needing to be brushed and stopping me from living out my dreams of biting directly into ice cream. I wish I could eat them.”? No? Well, thanks to Miss Terri’s amazing mouth adaptive gummy candy, now you can!"
You pause for a second to take this in, and then toss the pack in your pocket.
~trickortreat(gummymolars)
->officetop



===vatroom===
~Map_Magic++
The door in the office opens onto a flight of stairs, jet black marble leading down into more blackness, one second cousin to the void. Some where far, far below, something luminesces just enough to lend a greenish cast to the darkness.
You descend.

The room you find yourself in's only relation to the word is in the vastness it encompasses. A cavern, then, commanded by six enormous vats, bubbling and spitting and hissing louder than the roar of ship engines, casting strange shadows in stranger light across the dark floor and giving the bloody pulp on the floor a new, terrible life.
    Not wanting to spend any more time in this place than necessary, you dart forward and make a reluctant investigation of the...remains. 
    The ID chip is just salvageable enough to tell you that the bloody smears on the stone were once Captain Corowitz, copyright Finch Cybernetics. You do a quick count. Three bodies. Which leaves--you consult the dossiers Threefra gave you-- Catwin Evans, and...Penny Whittaker.
    You look around again. Something among the blood catches your eye. Slowly, you bend down, and when you rise Penny Whittakers lucky necklace is dangling from your fingers.
    You stare at it, entraced; you've never seen a real old Terran coin before. It's almost enough to make the viscera on your shoes worth it.
    ~pickup(pennysnecklace)
    ~questtoggle(Lucky_Penny, half)
    +[look around some more]
        Braving the maddening room a little longer, you poke around. There's something you can't quite put your finger on, until the metaphorical lightbulb suddenly flashes magnesium bright in your brain, and you start wondering why, this far underground, you're smelling clean, fresh air.
        It doesn't take you long to find the air vents. It takes you even less time to realize that, if you wanted to, you could easily fit through them.
        **[climb into the ventilation system]
            Curiosity may have killed that cat, but you've got a bona-fide feline resurrectionist on call. You make sure all your gear is secure, and forge on, all engines blazing and full steam ahead. Hopefully not literally, because for all your blithe immortality, you don't particularly enjoy being cooked.
            ->airvents
        **[definitely don't do that]
        Nothing good ever comes of climbing around in industrial circulation systems. You head resolutely back up the stairs.->offices
    +[go back]->offices
    


===airvents===
~Map_Magic++
It takes you far too long to realize that the slick substance under your fingers isn't oil, or grease. It's blood. And you realize, too, with a prickling, primordial dread of the rabbit when the hounds are running, that you're not alone in here. 
The snick-snick-snick of claws above you--no, to your right--no, behind you--and you drop flat and twist, half turning around to see gleaming augmented eyes and the smile of a rabid panther.
Catwin Evans. The bioengineered shapeshifter.
"Hello, prey," she purrs. "Wanna play chase?"
+[stand and fight]
    {shuffle:
        -Alas, you are not equipped for hand-to-hand combat with a woman capable of growing claws. Death comes swiftly, and horribly.->END
        -Somehow, someway, you manage to get a fatal strike in. Not without significant damage to yourself, but you're getting quite inured to appalling injuries. As you begin to drag yourself away,->airvents.um
        }
+{Inventory? MashaGrenades} [use Masha's Grenades]
    {shuffle:
        -Masha's home-ades are 49% unstable. This may be good odds in the lab, but not for you, particularly not when the blast is contained in a very small area, like an air vent. You can hear Nibu cackling as your body vapourizes in a spray of superheated organ tissue.->END
               
               -You've got the hand-eye coordination of pickpocket, and even better luck.->kaboommktwo
    }

+[try and escape]
    {shuffle: 
        -You get away clean, and drop out of the vents in the atrium.->entryhall.atria
        -Unfortunately, humans may be persistence hunters, but they are not designed to outrun genetically modified shapeshifters. Oh well. At least it's quick, this time.->END
    }


        -(kaboommktwo)
        ~questtoggle(Bangers_and_Masha, complete)
    The air flow is with you, and the vents direct the blast at Catwin Evans as she launches herself at you. Then, of course, she launches at the walls, in about a thousand different pieces. You're pretty sure there's a tooth lodged in your face, but frankly you'd rather not deal with that right now. As you scramble forward, coughing, 

        -(um) your hand lands on something small and metallic. It's a key--one of the analog kinds. You stare at it for a moment, and then slide it in your pocket with the rest of your finds. Now all you have to do is find a way out of these damn vents.
        ~questtoggle(tourist_bus,three_quarter)
           ~pickup(key)
        Eventually, you do, and find yourself dropping from the ceiling into the candy room. 
        {candyroom && lab_safety ? on: You have a thought about what that key might be for. }
    ->candyroom
 


===caskofam===
~questtoggle(Map_Magic, complete)
~questtoggle(tourist_bus, complete)
~questtoggle(Lucky_Penny, complete)
The key works. The door in the wall swings open, to reveal a depression just large enough to accomodate a medium sized human, and the slightly-larger than medium human it is in fact accomodating. He makes a move to lunge at you, before catching himself.
"You're not...you're not Catwin."
"Pennsylvania Whittaker, I presume?" you say. "Today's your lucky day. I've been sent to rescue you."
And that, of course, is your cue to leave, Penny Whittaker, white-faced and dazed, stumbling after you.

The recon base is much as you left it.
->explore
