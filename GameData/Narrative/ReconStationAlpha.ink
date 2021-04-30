
INCLUDE TheActualFactualFactory.ink
INCLUDE QuestTexts.ink



->begin
CONST playername = "Wednesday Adams"
LIST Inventory = (originstate), MashaGrenades, safety_goggles, mappingsoftware, pennysnecklace, codex1, codex2, codex3, emptyjar, notefile, key


LIST CandyBag = (empty), meatiorites, ohnoogat, mintalls, toughtoffee, liquorish, marshmellows, betabits, gummymolars, nukeproofcakepops, surebetsherbet, hotchocolatedrops, full


LIST QuestState= (not_recieved), recieved, quarter, half, three_quarter, complete, over

VAR Bangers_and_Masha = not_recieved

VAR Lucky_Penny =  not_recieved

VAR Map_Magic = not_recieved

VAR tourist_bus = not_recieved

VAR halloween = not_recieved


LIST itemequip = (off), on

VAR grenadestate = ( )
VAR lab_safety = ()

===function trickortreat(x)===
~CandyBag += x

===function pickup(x)===
~ Inventory += x

===function drop(x)===
~Inventory -= x


===function equip (ref itemstate, new_state) ===
    ~ itemstate -= LIST_ALL(itemequip)
    ~ itemstate += new_state

===function questtoggle (ref quest, state)
~quest -= LIST_ALL(QuestState)
~quest += state

==begin==
You land in dead terrain, all bone-white rock and wind. There could be a whole city here, but you'd never know; the stone looms up in towering canyons and mesas around you, blocking all but the howling wind.   
    "So where exactly are the heat signatures you're getting?" you ask Nibu over the comms.
    "Three kliks towards Beta Quellian," comes the prompt reply. "In your depressingly three dimensional terms, on your five."
    Turning, you squint into the gale, particles of sediment scraping your cheeks. The canyons turn this whole area into a wind tunnel, and you don't like it one bit.
    "Oh, and there's one right behind you," Nibu adds, just before someone--or something--presses something very sharp and very fatal to your throat.
    "You have three seconds to explain what you're doing here before I cut your throat," a woman's voice drawls. You recognize the slightly autotuned tones of a Finch copyright.
    "Leaving! Just leaving!"
    A pause. And then, "Who are you working for?"
    "My contract," you say warily, "is currently under dispute."
    "Hmm. Well. Technically my orders are to kill intruders on sight, but...I might just have a use for you. Interested in getting out of here alive, contractor?"
    You'd nod, but as you're against self-decapitation, on the whole, you make sure to enunciate a clear 'sure thing' and keep your head very, very still.
    "Great." The knife is suddenly removed, and you turn, to see a well-muscled Finch Security Operative in a sleek recon suit and the most cynical stare you've ever seen. "Come with me. We'll talk back at HQ."
    
    
    ->MAIN_Quest

  
    
    ==MAIN_Quest==
    HQ turns out to be a small base in the hastily shielded tunnels that apparently honeycomb the canyon. There's some surprisingly high tech stealth equipment, too--not good enough to fool Nibu, of course, but it's a curiousity.

"Right," your escort says. "Listen up, because I don't like repeating myself. The name’s Threefra Scalarian, copyright of Finch Cybernetics. I’m nominally in charge around here. That's all you need to know about that. And as for what <i>you</i> can do for me, and the continued continuity of your vertebrae, well, here's the deal.

    If you head about two kliks east, you'll find yourself tripping over a rather suspicious industrial complex. That’s all the very private property of Miss Terri’s Sugariffic Snack Company. No tresspassing, corporate espionage strictly forbidden, you get the idea. Now, my bosses, they don’t like things being private. They like to know. And completely unrelated to that, they happened to know of a little group of…let’s say, tourists, who decided to go on a nice holiday over there and snap some lovely keepsake photos of the scenery to show their bosses when they get home. Wink wink." Her expression is an extraordinary study in nothing at all. "You follow? And it so happen that these…tourists, well, let’s just say there's been no souvenirs in the inbox. And we, and by we I mean you, if you don't want your name in the reports and your body in the recycler, are going to go pick them up. Wherever they may be. And however many pieces they may be in. Got it?"

    *"Got it. Wink wink." 
        Threefra grunts. "Need any more info, or are you the rare miracle who doesn't require handholding?
        ** "Hands free all the way, ma'am."
        ->addendum
        ** "Actually, I have a few questions..." ->questiontop
    *"So all this secrecy and threats is over some lost tourists? Wow. How badly did you fuck up to get lumped with an assignment like this?"
		Threefra sighs. "Well, clearly I've fucked up something in my life to be subjected to this kind of idiocy, but no." She sighs. Those are very special tourists, conscript. What with all the intense paramilitary training and very expensive augmentations and gear that could theoretically be used to spy on a rival company, if our bosses at Finch would ever consider stooping so low as espionage, which they certainly would not. Am I making myself clear?"

		** "Oh. Ha. Right. Yes. All clear."

			Threefra: Good. Any questions? ->questiontop

		** "Oh. You could have just said that in the first place."
			
			Threefra's expression does not change. "Clearly. Is there anything else you need clarified? Maybe how to put your boots on the right foot?"

		** "I’m starting to think they aren’t really tourists at all…"

			"…any other brilliant insights? No. Really. I'm breathless with anticipation."
 
 -(questions)
+"Actually, I have a few questions…"
    --(questiontop)
	**"Miss Terri’s Sugariffic Snack Company? Never heard of them."
	-> MTS

    ** "So, is there anywhere for me to stock up before I go?"
	    A grunt. "Well, there’s Masha over there, but I wouldn’t trust anything she gives you that doesn’t have a patent stamp on it. Other than that, I can only hope you weren't fool enough to land in unknown territory completely unarmed.It wouldn't bode well for you survival." ->questiontop
    ** "Threefra? That’s an odd name."

	This is clearly a frequent question, at least from the exasperated exhale it recieves. "At least I’m not called {playername}. Look, we can’t all come from money like “Mr New York” over there. My parents were debtors. They couldn’t buy a proper name. So raising little “Try-new-ColferV-flash-bangers-three-for-a-credit” meant they at least had the credits to feed me. And it’s not like I’m going to go around calling myself ‘banger’. Yes, I could buy a new name. And no, I’m not going to." ->questiontop


** "Anything else I should know?" 

-(addendum)
"Oh, yes, before I forget, we've got a couple of Very Important People cluttering up the place," she says. "The man’s Nuyork Whittaker, the great nephew of Finch’s owner and CEO, Mr. Epps. And his son, Mr. Epp’s great, great nephew, was one of the, uh, tourists, we’re tasked with retrieving. Credits know how he thinks stalking around here is going to help, the officious parrot, but seeing as he doesn't just <i>think</i> he owns the world, we've got to play nice. So try and bring the kid back alive, eh?"
	
	** "I’ll do my best." 
	->exuent

	** "What if he’s already dead?"
		 
		"Then you bring whatever’s left of him back and hope it’s enough to clone him from."
		->exuent

	** "I’ll try and bring everybody back alive, regardless of their genetics."
		
		"That’s the spirit."
		->exuent

--(exuent)
"Right." She taps something on her arm tablet. "I've just sent you the last message we recieved from the tourists. Credit know how that'll help you, but there it is. Good luck, kid. Don’t die out there."
    ~pickup(codex1)
    ~questtoggle(tourist_bus, recieved)
    ++[explore the base]->explore

    =MTS 
        She gives you a stare so flat it's practically two dimensional. "Understandable, since they’re only the third largest corporation in the known universe. Oh, and the only supplier of 90% of mankind’s pharmaceuticals. And make literally every popular snack food known to man. Didn’t your parents give you those VitaBlast! drinks to protect your bone density when you were a kid? The ones that are supposed to taste like some weird fruit from old Terra? Yeah, Miss Terri’s makes those too."
	        **"Wow, Miss Terri must be raking in the credits."
	    	A shrug at that. "Who knows what Miss Terri does? No one’s ever seen her. There was a rumour going around on the Link that her board of directors had the real Miss Terri killed way back when, and when they turned on the replica AI they’d built to replace her, it saw its own dead body on the floor, went mad, and slaughtered everyone in the building. They say the AI Miss Terri still lives in those offices, holding meetings with the dead executives and periodically sending out formulas for new products.
		    Good story, eh? I don’t buy it, though. No AI’s killed a human since, oh, the oil ages."
		    In your ear, Nibu snickers. 
		    Threefra sighs. "Any less stupid questions before I send you skipping off down the flower path to evisceration?"-->questiontop
        	** "I was kidding. Of course I know what Miss Terri’s is!"
	         "I should bloody well hope so. Anything else?" -->questiontop
    


 
=mainquestundone
"Unless you're here to tell me that you've braved Terri-torry incognito and saved the day and the founder's great-great nephew, I don't want to hear it." 
Wisely, you retreat. ->explore

=tisdone
{tourist_bus ? complete && Lucky_Penny ? complete:
Threefra studies you, head cocked on one side like a wary sparrow. "So you’re saying it was Catwin Evans? Well. I can't say I'm entirely surprised. She was practically rabid even before she got all the augments. Made a killer steak, though." She coughs. "Possibly in bad taste now, huh? Oh well. Good job on bringing the kid home. And you, my friend, are free to go. And if you're ever in need of a spot of contract work, look me up. We could use someone like you at Finch."
    ~questtoggle(tourist_bus, over)
    ->explore
}

{tourist_bus ? three_quarter:
Threefra studies you, head cocked on one side like a wary sparrow. "So you’re saying it was Catwin Evans? Well. I can't say I'm entirely surprised. She was practically rabid even before she got all the augments. Made a killer steak, though." She coughs. "Possibly in bad taste now, huh? Oh well. You say you didn't find the kid?"
"Just his lucky necklace," you say.
"Not so lucky, apparently," Threefra says, and shrugs. "Oh well. Come down to that, they can always make more Whittakers."
"That's what his mother say," you observe, and make your exit.
     ~questtoggle(tourist_bus, over)
    ->explore
    }

{tourist_bus ? over:
"Oh, you're back for more, are you?" Threefra looks up from the report she's idly perusing. "Well, can't help you now. Try back later." ->explore
}
    

     ==explore==
    *{Inventory ? codex1} [read the "tourists'" last report]->codexthefirst
    *{Map_Magic ? not_recieved}[map room] ->Survey
    *{Map_Magic ? recieved}[map room] ->Survey.survey_unfinished
    *{Map_Magic ? half} [map room] ->Survey.survey_done
    *{Map_Magic >= half} [map room] ->Survey.survey_done
    *{Lucky_Penny ? not_recieved} [mainroom] ->luckypenny
    *{Lucky_Penny ? recieved} [mainroom] ->luckypenny
    +{Lucky_Penny ? three_quarter && Inventory ? pennysnecklace} [main room] ->luckypenny
    +{Lucky_Penny ? complete} [mainroom] ->luckypenny
    *{Bangers_and_Masha ? not_recieved}[hallway]->masha
    +{Inventory ? MashaGrenades && Bangers_and_Masha ? recieved} [hallway] ->masha.MashaQuestIgnored
    *{Bangers_and_Masha ? complete} [hallway] ->masha.MashaQUESTDONE
    +{Bangers_and_Masha ? over} [hallway] ->masha.MashaNoQuest
    +[mess room]->canteen
    *{tourist_bus ? recieved} [talk to Threefra] ->MAIN_Quest.mainquestundone
    +{tourist_bus >=three_quarter && tourist_bus <=complete} [talk to Threefra] ->MAIN_Quest.tisdone
    +{tourist_bus ? over} [talk to threefra] ->MAIN_Quest.tisdone
	+[leave the camp]
	    ++[return to ship] ->DONE
	    ++[head for site] -> outbuildings
	
	==canteen==
	The mess hall is empty. You glance around surreptitiously, and swipe a carton of Tutti-Frutti NutriMilk. You've got a lot of work ahead of you, after all.->explore
	
    ==masha==
      
As you approach the not-so-far side of the small station, you are greeted by a deafening bang, followed by the screech of life support alarms and a gust of acrid smoke. Before you can explore further (or, more wisely, retreat back past the jury-rigged blast door), a figure in safety goggles and fireproof gloves tumbles out of the smoke and whoops loudly. 
"Oh yeah, baby! Smoke ‘er up! Just look at that spread. You could take out an entire station with one of these cuties!"
        * Coughing violently, you manage a rasped "Uh, I’d rather you didn’t."
            Masha: Oh, uh, hi! Didn’t think anyone’d be back here! I mean, I put up a sign.->sign
    	* This won't do. You clear your throat. "I’m fairly certain this is against station testing protocols. I’ll be reporting you."
    	    She grins. "To who? The people who're funding all this?"
    	    That's a good point. You shrug. ->hi
	    * You feel the corners of your mouth curling up in a grin that's been absent since Threefra collared you tresspassing. "Smoke? Explosions? Poor lab safety? Count me in!"
		     The apparition semms slightly offended--or possibly just concussed. "Poor lab safety? Excuse me. Do you see these? Safety goggles. Safe! It’s right there in the name!" Anyway, what are you doing back here? I put up a sign.->sign


 
   -(sign)
        *"Uh, the one that said ‘Danger: telepathic lab mice’? I thought that was a joke."
		"That’s what they want you to think." She wags a gloved finger at you. There's at least one hole burned all the way through the carbon nanofibers. You <i>really</i> don't want to know how she managed that. "But no. I meant the other one. The one that says: ‘If you smell smoke, don’t choke! Always switch on your life support implants!’ 

		    ** "…There were no explosions mentioned anywhere on that sign."
		    "The explosions are implied. The explosions are always implied."
		     You can't argue with that. The woman grins and waves her hand, an action that spreads the smoke rather than dispersing it.
		    ** "I...see." ->hi
    * "I didn't see a sign anywhere."
        She looks sympathetic. "Oooh, forgot your safety goggles one time too many, eh? Yeah, that'll get you." She pats your arm.->hi
		

-(hi)"Well, never mind. Dr. Masha Themisen, local chemist on call, intellectual property of Finch Cyber. But everyone calls me eyebrows. Well, they would, if I had any, haha! And you’re the snooper ol’ Threefra nabbed, right? And I don't doubt she's "volunteered" you for a mission. For the missing recon team--I mean the tourists. Oh dear. But I don’t see why we have to be all cloak and taser about it. It’s not exactly a secret if everyone already knows, is it?" 

The smoke, instead of dissiapating, seems to actually be growing thicker.

"Oh, but since you’re back here anyway, you sign-ignoring daredevil, you, maybe you can help me out! I’ve got some more of these grenades lying about…somewhere…now where’d I…? Aha! Shoes! Always keep your grenades in your shoes. Keeps ‘em grounded."

*Are you sure you’re really a scientist?
    She looks offended. "Are you sure you're actually a human?"
    That's...a question you'd rather not answer, given Nibu's recent revelations. "No," you say anyway, because you shouldn't be the only one confused here.
    
* Ha! That’s great! I’ll have to remember that!
 "Please do! because I never can." Clicking her heels together--you flinch instinctively--she adds "good thing I've got bionic legs, right?"
 You can only agree that this is indeed very sensible, given the circumstances.
 ->expo
*With all due respect, I think I’m going to leave those explosions implied for now.->explore

-(expo)Adjusting her safety goggles--a maneuver that seems to require a great number of unlikely facial contortions, she goes on. "Right, well, I’ve been making these grenades—I’ve been calling them homades, because they’re ‘homemade grenades.’ Get it? Yeah, Threefra said it wasn’t funny either. Still. I’ve just been doing small scale tests right now, but what I really need is some proper combat data! And since you’re already heading out into the great unknown on a humanitarian mission--wink wink--I thought you could just, you know, take some along, chuck 'em at a few specimens of local wildlife, count the left over limbs, you get the idea! So how’s it sound?"

+ "Sounds like a blast!"
    "You and I," she says, placing a still slightly-smouldering hand on your shoulder, "are going to get along <i>great.</i>" 
    ->accept
+ "Exactly how unstable are they?"
"Oh, hmm, about 71%. Those are good odds in explosives!" 
You're...not sure this is true. Still.
"If you're really worried," she continues, "though of course there's no reason to be! You'll be armed with the best handheld explosives on the planet! But if you're still not sure...I'll tell you what." She digs around in the voluminous pockets of her lab coat, muttering, before coming up with a battered old pair of safety goggles, crumbs dusting the lenses like edible shrapnel. "They're a little, well, lunchy, but they'll do. Just be sure to bring them back, because I'm running out of them." 
    ~pickup(safety_goggles)
     **[put them on]  
     ~equip(lab_safety, on) 
     [safety goggles equipped. You're a paragon of lab safety!]
     ->masha.forth
     **[shove them in your pocket] ->masha.forth
    -(forth) "So how about it? Now we're all eyeball safe and up to code."
    ** "Alright, then." 
    ->accept
    ** "That's still going to be a no from me." ->masha.nowayjose
+ yeah, hard pass.
    -(nowayjose) She shrugs philosophically. "I tried. Oh well. If you aren't going to help, would you do me a smaller favour and get out of the blast zone? I hate having to sterilize everything after someone gets spattered all over everything.
        "Aw, you should have agreed," Nibu says over your com. You can *hear* the pout in her voice. "It's not like I can't just reset you if you run into terminal difficulties."
        "Do you have any idea how painful being explosively dismembered is?"
        "No. I don't have that data." A suddenly hopeful not appears in her voice. "Do you think you could collect it for me? I could install a tracking program to measure nerve response to various stimuli. It'd be fun!"
        It would not be fun <i>at all</i>, and you turn the volume all the way down on your copilot before she convinces you otherwise.
        ->explore

-(accept) Waving her hands, all live circuits and ignition, she turns and begins bounding back down the hallway away from you, calling over her shoulder as she does. "I’ll get those all ready for you! I’m so excited." 
She's back in a flash, pressing a suspiciously fragile looking box into your hands. "Here you go! I wish I could come along, but, well, I’m banned from all live operations since the incident with the mini fridge. Still, never mind. Have fun! Blow something up for me!"
    [recieved Masha's Grenades]
    ~pickup(MashaGrenades) 
    ~questtoggle (Bangers_and_Masha, recieved)
    With that, she's gone as fast as she arrived, and you--very carefully--stow the explosives and contemplate your next move.
    ->explore


    =MashaQuestIgnored
    It's suspiciously quiet in the hallway, a single curl of smoke drifting behind the haphazardly piled sheetmetal optimistically designated a "blast door".
    "Oh! Hey! It's my new best friend! Did you test the homeades yet?"
    You shake your head. Beneath the safety goggles, the foreboding of a frown begins to gather. "Well? What are you doing here, then? Go blow things up!"
    You nod soothingly, and leave before the chemist can decide to skip the middleman and test her grenades on you.
    ->explore
    

    =MashaQUESTDONE
    ~questtoggle(Bangers_and_Masha, over)
    You peer around the corner warily. The jury rigged blast doors blocking the hallway are...no longer doing much of a job of blocking it. 
    
    
    Before you can beat a wise retreat, Masha comes careening out of a doorway, in full PPE and bringing with her the aroma of...fresh baked cookies? She hands you one almost absentmindedly. "So? How’d it go?"

    * "Have you ever heard of the term ‘collateral damage?’"
    She squints are you through the scratched and stained lenses of her safety goggles. "Your collar looks fine to me." Then she grins. "I kid! I know what collateral damage means! I make the means that provide the collateral. And you, my friend, have provided that damage. Oh, well done. I appreciate your dedication to science!
	*"Well, they’re incredibly unstable, scorched me about as much as my enemies, and created a shockwave that concussed everything in a hundred yard radius…and I’m going to need five hundred more of them immediately."
	"That," says Masha, "is the best news I've heard since the Great Garbanzo started selling cobra-oil toothpaste. You have no idea how much I appreciate this!"
	 *"My lawyers have recommended that I not answer that question on the basis of self-incrimination."
	 A brilliant grin at that. "Huge amounts of collateral damage, huh? So what you’re saying is that they’re too good. I get it. The corps are so fussy about things like that. No respect for the scientific process at all. Sighs. Well, never mind! Thank you so much for your help! It’s nice to see someone who understands the critical importance of really great explosions in space exploration."

    -(endmasha) [quest reward entered into inventory] 
    With one last, loud "Thank youuuu!", Masha pumps your hand enthusiastically--it burns slightly--and bounds away, the soles of her boots flashing steel. You smile, rub your scorched shoulder, and munch on a surprisingly delicious cookie as you consider what to do next.
    ->explore
    
    =MashaNoQuest
    "Oh, you're back!" Masha says, waving excitedly. "Sorry, I'm a little busy here. But if you ever want to test some more explosives, I'm sure I'll have these refined and ready for beta testing soon!"->explore
   
    ==Survey==
	The room you find yourself in is small, extraordinarily neat, and completely overshadowed by the massive robotic figure looming in the corner like a space-age colossus. When the little man behind the desk speaks, it takes you a moment to even register his existence.
	"Oh! What? Good gold, don’t sneak up on people like that! You could give someone an aneurysm! And not a hospital station in a day’s flight…It’s criminal, is what it is." His face, a curious little ratlike aspect, is an alarming shade of red. "Yes, positively <i>criminal</i> work conditions, and on a salary that only lets me afford a mass market immune system…Masha over there—horrible woman, always covered in all kinds of powders and things—well, she even told me they just had a case here of suppurating space vacuum fever, with all the squelching and the oozing and the sudden explosive decompression—gah! 
        * "Uh, I think Masha might be pulling your leg a bit."
        "I wouldn't let that...pyromaniacal stochiometrist anywhere near my limbs. She bakes cookies in the autoclave! It should be illegal. It is illegal! There's a patent violation in there, I know there is. And you know how the patent enforcers are, always with the door-kicking and the hammers and the precision lasing of the vital organs--agh." Hopping around on one foot like an infuriated flamingo, he spins on the colossus in the corner, howling. ->into
        *[say nothing]

--(into) "ZANE-X! Where are you? Useless robot. Supposed to help my nerves. There’s nothing wrong with my nerves, it’s my spleen I’m worried—"

A voice, sized to match its owner, reverberates through the room. You've never been more grateful for the volume slider Nibu added into you implants on a whim. (You're less grateful for the one that mutes <i>you</i> at the AI's mercurial pleasure, but beggars can't be choosers.) "BE ASSURED, MR RAISIN. I AM PRESENT, AS IS MY SCRIPTED DUTY AND ONE WHICH I AM PROGRAMMED TO TAKE PLEASURE IN." 

The man at the desk covers his face with his slim, long-fingered hands. They are an artists hands, and you wonder what this nervy little creature is doing here, in the middle of no man's land with a specialist recon team. "It’s Mr. Ryson. RY-SON. It’s not that…oh, it doesn’t matter right now! ZANE-X, I just need you to run a diagnostic scan on my spleen. I’m sure I felt it oozing just now—

    "I AM SORRY, MR WRITE-ON, THAT IS NOT WITHIN MY VAST RANGE OF CALMING SERENI-CISES, COPYRIGHT FINCH PSYCHOSOMATICS (A SUBSIDIARY OF FINCH CYBERNETICS). IF YOU WISH TO DOWNLOAD MORE WONDERFULLY SOOTHING SERENI-CISES, PLEASE VISIT YOUR NEAREST FINCH REPRESENTATIVE. (THE USE OF THIRD PARTY SOFTWARE IS STRICTLY PROHIBITED AND WILL RESULT IN THE ACTIVATION OF MY SELF DESTRUCT SEQUENCE AND THE IMMEDIATE DISPATCH OF THE FINCH LABORATORIES PATENT PROTECTION SQUAD.) CAN I INTEREST YOU IN ONE OF MY PREPROGRAMMED ROUTINES?"

    "You come with a scanner, you useless hunk of metal!" The fingers are tapping on the desk now, a quick tick-tick-tick of muscle-memory on invisible keys. "We went through this all yester-" 

    "AFFIRMATION DETECTED. INITIATING SERENI-CISE NUMBER 456."

    "I said yesterday, you daft—"

    "SILENCE! QUIET IS REQUIRED FOR THE CULTIVATION OF TRANQUILITY. NOW MR RIBOSOME, PLEASE UTILIZE YOUR HUMAN CREATIVE FACULTIES TO IMAGE A SOFTLY BABBLING BROOK. IT IS A VERY CALM BROOK, COMPRISED MAINLY OF COOL AND REFRESHING DIHYDROGEN MONOXIDE, WHICH IS NECESSARY FOR YOUR CONTINUED SURVIVAL. SMALL CALM FISH PERFORM THE ACTIONS DEMANDED BY THEIR NATURAL INSTINCTS, UNTIL THEY ARE EATEN BY LARGER FISH, AS THEY IN TURN ARE EATEN. SUCH IS THE LIFECYCLE OF THE MINNOW. WE, ABOVE, REMAIN SERENELY OBLIVIOUS TO THE MEANINGLESS DRAMAS OF PISCINE SUFFERING. FOR US, THE BROOK IS CALM. ARE YOU FEELING CALM NOW, MR ROSENBAUM? IF YOU REQUIRE FURTHER TRANQUILIZING I AM CAPABLE OF MANY OTHER SOOTHING ACTIVITIES, SUCH AS DEEP TISSUE MASSAGE AND CONCUSSIVE CHIROPRACTORY."
    
    Eying the massive articulated chrome hands of the robot, you sidle towards the door. Collateral damage is not your prefered occupation.

    The man rubs his temples. "What? None of those things sound calming! Oh good gold…how am I supposed to work under these conditions? And the danger! None of this is in my contract! But they’re expecting their survey report, and I wouldn’t want to fall afoul of..well."

    Mr. Ryson's sharp little eyes narrow suddenly, to focus with prey-animal intensity on you. "Wait a minute. You there. You’re a gun for hire, right? All this dangerous exploring and shooting and exposure to toxic chemicals is what you’re here for. Listen, I’m supposed to be doing a full geographical and chemical survey of whatever's out there. Normally I’d be collecting data points myself, but since those tourist fellows went missing—I’m a cartographer, you understand? I don’t deal with all this exploding business. And the air on this planet dries out my skin something awful. But you…yes. You’re practically designed for this stuff! I’ll just program everything into this handy little gadget, and it’ll do all the data collection for you. All you have to do is walk around the area and try not to die. The more data you get, the better, of course!"
    ~pickup(mappingsoftware)
    [surveying gizmo added to inventory]

The nimble fingers fly across the input screen of a handheld device the size of your palm, and you find it pressed into your own hands before you can object.
"There you go!. All set. Now, ta ta, get those nice healthy organs pumping, time’s a-wasting. I have to send an update message—

ZANE-X: MASSAGE ROUTINE ACTIVATED

RYSON: No! That’s not! Aaaagh!
~questtoggle(Map_Magic, recieved)
Stowing the survey tracker, you make a hasty and unadvertised exit. ->explore


    =survey_unfinished
    You peer warily into the map room. Ryson is crouched behind the desk, adjusting a dial on a laser with the only vestige of calm you've seen in him so far. You decide to let him be, and withdraw, unnoticed.->explore


    =survey_done
    {Map_Magic ? quarter:
    No sooner have the sound waves of your footsteps raced past the door to the ears of the man within, then he's bounding over, eyes bright. "Well?"
    You hand over the survey tracker. As Ryson scans the quickly scrolling data, his expression grows more and more distressed.
    "No, no, this won’t do at all! I could barely plot an empty room with this! People are waiting on my mapping, you understand? I have a brand to uphold. Go back out there and get more data immediately!"
    Blinking, you find yourself already shoved unceremoniously back into the corridor. The door slams. As you turn away, a little irked, you hear ZAN-X begin expounding on the inevitable heat death of the universe at wall-shaking volume. ->explore
    }

    {Map_Magic ? half:
    Entering the map room, which is, somehow, even more unnervingly organized than before, you hand Ryson the survey tracker. Fingers flash; the screen flashes too. He puffs up his cheeks like a dyspeptic chipmunk. "Is that really the best you could do? Hmmph. Well. I’ll manage, I suppose. I don’t need the plotter to do all my work for me, unlike some I could name…well. Thank you. I guess. Now get out. I have a lot of work to do."
        ~drop(mappingsoftware)
        
            [min quest reward deposited] 
             ~questtoggle(Map_Magic, over)
             ->explore
    }

    {Map_Magic ? three_quarter:
  After flicking through the data you've gathered, Mr. Ryson actually smiles. "Oh! Wonderful! Yes, yes, I can work with this. Thank you! You’ve saved my reputation…and probably my organs too. No! I didn't ask for "organ music", you worthless pile of bolts! Why would I ask that? No one would--"
  ~drop(mappingsoftware)
    As the earth-shattering prelude to an oratorio begins to swell, you quickly turn your external volume to "mute, for the love of all things profitable, mute," and flee.
    [max quest reward] 
    ~questtoggle(Map_Magic, over)
    ->explore
    }

    {Map_Magic ? complete: 
    Satisfied with the exacting thoroughness of your data gathering, you hand over the survey tracker. 
    ~drop(mappingsoftware)
    Much to your surprise, after flicking through a few screens, Ryson's narrow face begins to fall. "Oh! Well. This is…this is great, I suppose. But…it doesn’t leave much room for interpretation, does it? I mean, what am I here for? The people like their maps slightly vague, you know. Creates a sense of mystery, of the great unknown, the last unexplored frontier!…well, I don’t suppose you’d understand that. But you’ve more than earned you reward, I guess."
    [max reward deposited, begrudgingly.] 
     ~questtoggle(Map_Magic, over)
     -> explore
     }
     
     {Map_Magic ? over:
     You've barely set foot in the corridor outside the map room before Ryson is screaming for you to get out, leave him alone, he's too busy for interruptions and you'll give him <i>palpitations</i> and--
     You leave him to gnaw his own nerves. ->explore
     }d f

   
    
	
	==luckypenny==
	    Peering into the largest room of the small base, you're startled to see it occupied by a clearly executive couple, a tall man in a very expensive suit and the air of one used to having his orders obeyed, and a woman all elegance and ennui. This, you figure, must be the famous Nuyork Whittaker, and the woman, then, his wife. Or mistress, you don't judge.
	        "Wife," Nibu drawls in your ear. "Melia Whittaker. I can't fathom why you humans insist on clinging to that archaic idea, but then, I am but a mere AI."
	        "Property rights," you say, shaking your head. It's not an entirely unfond gesture. "<i>Everything</i> eventually comes down to property rights."
	        "Property is theft," Nibu says promptly, and ends communication. You are beginning to regret giving her access to your library of not-quite-legal old screenflips.
	           -(option)
	            *{Lucky_Penny ? not_recieved}[talk to Nuyork Whittaker] ->nuyork.questget
	            *{Lucky_Penny ? recieved}[talk to Nuyork Whittaker] ->nuyork.why
	            *{Lucky_Penny ? three_quarter}[talk to Nuyork Whittaker] ->nuyork.pennyfound
	             *{Lucky_Penny ? complete}[talk to Nuyork Whittaker] ->nuyork.pennyfound
	            *[Talk to Melia Whittaker] ->melia
	            + [leave] ->explore
	    
	    =melia
          Melia Whittaker eyes you up and down, her eyes hooded and clever. "My son? Oh, yes. Penny. Well, if his father asks you to find him you may as well. We’ll have no peace til his precious little boy comes home safe." She laughs. "Like as not Penny just skipped out on whatever silly thing Great Uncle Colorado asked him to do to go gravity racing with his friends. But my husband can be a little…overprotective. Best to do what he asks. And if it turns out Penny met some nasty fate gallivanting around in the backwaters of the galaxy…well, you know. I’m fond of my son, of course. But it’s not like we can’t make another one. I really always wanted a daughter anyway." ->option

	    =nuyork
	    -(questget)
	    As soon as he catches sight of you, the man wastes no time getting to the point.
	    "Hey, you! Listen here. I don't know who you are, but you look like you can handle yourself. My boy—my Penny—he should have been back days ago. I don’t know what damn fool task his great uncle sent him on, but he’s only—he’s just a little boy. This was supposed to be an easy run, just for him to get his feet wet. He wasn’t supposed to be in danger. He’s fragile, he’s not equipped to deal with—" He inhales shakily, voice hitching. You venture to pat his shoulder consolingly. "Well. I need you to bring him back to me, understand? Leave everyone else behind if you have to. They’re just contractors, right? There’ll be more of them. There’s only one Pennsylvania Whittaker. Bring him back safe, whatever the cost. I have the funds for it. And if you can’t…I…there’s a necklace with an old Terran penny on it. I gave it to him when he was a baby. A lucky penny for a lucky Penny, right? He never takes it off, not for anything. I…if you can’t bring me him, I need you to bring me that. As proof. And so I have—I have something left of him. Do this for me, and you’ll have the eternal gratitude of the Whittaker family. And a substantial reward, of course."
	        * "Of course," you say. You're not heartless, whatever Nibu insists. 
	        ->surething
	        * "No." You don't his suit, fibres woven on the backs of his workers, his blind assumption you'll do as you're told, and the implication that you and your ilk are disposable.
	        He looks shocked, and you grin, wave, and walk out. ->explore
   

    -(surething)The man seems a little calmer. He grasps your hand. "Thank you. Time is of the essence now."
    You waste no more time lingering to chat. It wouldn't do any good.
    ~questtoggle (Lucky_Penny, recieved)
    ->option
    
        -(why)
            You start towards the man, and then, as he turns hopeful eyes to you, think better of it, and make a hasty escape. ->explore
        
       
    
        -(pennyfound)
        {Lucky_Penny ? three_quarter && Inventory ? pennysnecklace:
        The moment you walk in, Nuyork Whittaker grabs you. "You’re back? So? What’s the...what..."
        His eyes fix on the necklace you're holding out to him, and you have the singular experience of watching a man's soul die.
        "Oh," he says. Then: "My <i>son.</i>"
        You say nothing. There's nothing to be said.
        ~questtoggle(Lucky_Penny, over)
        }
        
        {Lucky_Penny ? complete:
    The moment you walk in, Nuyork Whittaker grabs you. "You’re back? So? What’s the—is that—yes! It is! Penny! My little boy. My darling son. You’re alive!"
    Penny beams, and throws himself into his father's arms. "Dad! I’m so glad to see you. You’re not going to believe what I’ve been through. That place, it—it’s almost as unbelievable as Miss Terri’s new line of lithium enhanced snacks! All of the flavours you love, plus the healthy, balanced lifestyle you’ve been craving! Try them now, at select locations.

    His father's brow wrinkles. "…Penny?"

    "Huh? Sorry, I must have dropped off for a minute there." He turns to you, grinning broadly. "Uh, well…thanks for saving my life. I really appreciate it. If you ever need anything, just ask for Penny Whittaker, right?...Hey, Dad, do you think there’s anything to eat around here? I’m starving.

    "Anything you want, kiddo." He pats his sons shoulder, and then wrings your hand gratefully. He has the grip of a cabrian tree sloth. "Thank you. You’ve more than earned this."

    [quest reward enters inventory]

    "I could really go for a cake pop…" Penny says. Father and son begin walking towards the door, talking excitedly, and you, having served your purpose, no longer exist.
    ~questtoggle(Lucky_Penny, over)
    ->explore
    }










