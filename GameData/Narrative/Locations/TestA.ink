#constraint: DistanceFrom start = 0 // Place this location in the starting zone
#type: Station // Determines the type of entity for this location (Station/Asteroid/Planet)
#security: Open // Security level (Open/Secure/Critical)
#faction: Miss Terri // Locations should be tagged with the faction they belong to
#name: Test Location 1 // Will set the name of the entity as it appears in game
#nameZone: Start Zone // Will name the zone that this location is placed within - locations with named zones are excluded from being placed together
#turrets: 1 // Will place defensive turrets around the station

-> start

==start==
This is the start of the location story.

 + [Go to location A1] -> A1
 * This will end the story.

- They lived happily ever after.
    -> END

==A1==
This content is in location A1.
This is the second line of content in A1.

 + [Return to start] -> start
 + [Go to location A2] -> A2

==A2==
This content is in location A2.

 + [Return to start] -> start
 * End the story ->END