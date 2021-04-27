
==landing==
you've landed in the magical pepper marshes. what now?
*{pepper_state !? yes} [go into the marsh] ->the_pepper_patch
*{pepper_state !? yes} [chat with pepper whisperer]->pepper_whisperer
*[leave]
    {pepper_state ? yes} Nibu will not allow you to linger on the pepper planet. You hie away, because you like your nerves unextracted from your body.
    ->END


==pepper_whisperer==
the pepper whisperer says you can have one, if you spend a night in the marsh communing with the spice spirits.
    *sure, why not.->marshmallows
    *uhhh...no.->landing
    
    =marshmallows
    the marsh is damp, and humid. the spice spirits are certainly not as fun as the spirit spirits of olympus mons. still, you are not dead. the pepper whisperer congratulates you and gives you a pepper. you rather suspect he's laughing at you. 
    ~ get(pepper)
    ->landing
    
==the_pepper_patch==
peter piper picked a peck of pickled peppers, but you only need one.
*[steal a pepper] ->ohshit
*[eat pepper]
    You cannot eat the pepper. It breaks space time. Nibu resets the loop, and bans you from all further hot sauce ventures under pain of pain. 
    ~pepper_state = yes
    ->landing
*[leave]->landing


=ohshit
Stop, criminal scum! You have violated the law! The pepper police know neither fear nor mercy.
*[run]
    {shuffle:
    -you have the legs and agility and probably the preserved heart of an adolescent giraffe. Escape! ->landing
    -perhaps you should not have skipped leg day. its the capsaicin mines for you!->END
    }
*[fight]
    {brunch_state ? fork: 
    you stab the pepper policeman and escape. well done, you murderer. ->landing
    -else:
    whoops. you done fucked up. ONE THOUSAND YEARS DUNGEON! ->END
    }