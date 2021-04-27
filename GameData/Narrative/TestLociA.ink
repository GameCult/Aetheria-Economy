===land===
We're on a planet! whoo. let's see what's here.->start


=start
What do you want to do now?
*[look around]
->snoop
*{ hunger_state !? not_hungry} [look for food] -> brunch
*[underground hobo fights!]-->fight
*leave ->END



==fight==
location specific quest. It's a hobo deathmatch.
    *fight?
       {shuffle: 
       -you win! congrats. You gain the great and mighty bent fork of foul ol' ron
       ~hunger_state = mild
       ~get(fork) 
       [fork added to inventory!] ->land.start
      
       -you lose. sucks to suck->END
       }
    *report this madness.->END

==brunch==
There's a space noodle stand. You once killed a man with a noodle. You contemplate a proper breakfast.
    *hunt up hot sauce
        not on this planet, my friend. you are informed of the magical pepper marshes of keysmash 6, so hot they reverse entropy. You yearn for the burn of thermodynamic impossibility.->land.start
    *find a space chicken for egg
        you find an egg balanced on a wall. 
        ~ get(egg)
        ->land.start
    *find potatoes
    oh no, my friend. potatoes are a rare delicacy. you must visit the caliph of ko and convince him to grant you potato privileges. ->land.start
    *find bacon
    no bacon either! you must hunt the wild rasherbeast of glug and harvest its sweet sweet flesh. ->land.start
    





==snoop==
what's all this then?->END