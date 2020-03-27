# Aetheria
This repository is the home of Aetheria, a sci-fi hybrid ARPG/RTS MMO about a group of corporations colonizing a hostile alien galaxy, with a satirical narrative critiquing late-stage capitalism.

## Table of Contents

1. [Game Design](#Game-Design)
2. [Previous Work](#Previous-Work)
3. [Current Work](#Current-Work)
4. [Architecture](#Architecture)
5. [Contributing](#Contributing)
    - [Getting the Files](#Getting-the-Files)
    - [Choosing a Task](#Choosing-a-Task)
    - [Codebase Concepts](#Codebase-Concepts)
    - [Database Editor Tools](#Database-Editor-Tools)
    - [Connecting to RethinkDB](#Connecting-to-RethinkDB)
    - [Editing Items](#Editing-Items)
6. [Galaxy Editor](#Galaxy-Editor)
    - [Map Layer Data](#Map-Layer-Data)
    - [Star Tools](#Star-Tools)
7. [Contact Us](#Contact-Us)



## Game Design

The ARPG game design document is available [here](https://docs.google.com/document/d/1iULu1WsbuQoUM3c87XkGseb1P-8R5xlruoiyg03TsSE/edit?usp=sharing), while the RTS gameplay is documented [here](https://docs.google.com/document/d/1U3uGFqQboAiFJ_Y-nUOGpyixbXUHRbc5DiCuB59GM4w/edit?usp=sharing). The goal is to essentially create two games which both take place in the same persistent universe, allowing players with vastly different preferences to struggle together for the survival of mankind. Each instance of the game lasts until the inevitable destruction of the entire population at the hands of aliens, after which the universe resets. Each loop is designed to last up to a couple of months, during which the hostility of the aliens steadily increases until the players are unable to hold back the tide.

## Previous Work

The concept for Aetheria goes back many years, during which I have steadily acquired my current skill with the primary objective of becoming competent enough to realize my vision. Previously I have built prototypes of the ARPG gameplay, [here's a video of the most recent one](https://www.youtube.com/watch?v=PNwVGtvefCg). While it included stations, AI opponents, multiple ships and a complex loadout system which simulates heat transfer between all of the ship's hardpoints with temperature affecting the performance of each item differently, the world was rather static and empty.

## Current Work

As a result of lessons learned, the current focus is on the economy system, and building a client-server architecture for the networked simulation of a persistent universe. The goal is to create an RTS client, allowing players to take the role of a corporation, where they can define roles for their population, gather resources, build infrastructure, research new technology and produce items in order to make as much money as possible.

Once we have built a persistent universe with a dynamic economy, we will begin rebuilding the ARPG client allowing the player to take control of a single ship and engage in fast-paced combat, questing and trading.

## Architecture

There are two solutions in this repository. One is a Unity project containing the desktop client for Aetheria, and the other is a .NET Core application intended to run on Linux cloud servers. There is some code shared between them, located at [Assets/Scripts/ServerShared](Assets/Scripts/ServerShared), defining a common protocol and data serialization format. Client-Server communication is implemented using [LiteNetLib](https://github.com/RevenantX/LiteNetLib), a reliable UDP transport library which we use to transmit [MessagePack](https://github.com/neuecc/MessagePack-CSharp) over the wire.

Aetheria uses [RethinkDB](https://rethinkdb.com/) for data persistence. To make this possible, all persistent data is marked with attributes for both MessagePack and [JSON.Net](https://www.newtonsoft.com/json) serialization. During operation, the client does not communicate with the database server directly, only the game server does that; the game server caches data relevant to the game and sends it to the clients.

## Contributing

### Getting the Files

In order to checkout the project, you need a git client (Github's zip download will not work!). You also need to have installed [Git LFS (Large File Storage)](https://git-lfs.github.com/). This is necessary because assets in gamedev projects can get rather large, and Git is essentially a text versioning system that does not by itself support that use case well.

### Choosing a Task

We are organizing according to an [Agile development](https://en.wikipedia.org/wiki/Agile_software_development) schedule, with the progress of each sprint being tracked on its own board in the [Github Projects tab](https://github.com/rwvens/Aetheria-Economy/projects). If you wish to take on a task from the board, please contact us to become an official contributor so that the task can be assigned to you directly. Some issues are not on the sprint schedule, those are ideal for developers who want to jump in but are shy about joining.

### Codebase Concepts

All objects stored in the database inherit from DatabaseEntry. More coming soon.

### Database Editor Tools

In order to facilitate the creation and maintenance of game data, there is a Unity editor utility which communicates directly with RethinkDB. You can access the tools by selecting Window/Aetheria Database Tools in Unity's menu. This will cause two windows to appear, the Database List View and the Database Inspector.

#### Connecting to RethinkDB

At the top of the list view there is a text field where you can enter the URL of the database server. When you click connect, the editor will download and cache all of the items in the game, as well as subscribe to the changefeed. The list should now populate with items. For access to our database servers and therefore live game data, please contact us; it would be dangerous to make our actual database URL public!

#### Editing Items

You can unfold the categories of items in the list view to see what items exist. If you select an item, the Database Inspector will populate with all of the available fields of that item. Any changes you make in the Inspector will automatically be pushed to RethinkDB. If you've connected to the production database, this will update the stats of in-game items in real-time!

### Galaxy Editor

When you select a galaxy asset in the Unity scene hierarchy, a custom editor opens in the inspector which enables the procedural generation of a new galaxy. There you can find some variables pertaining to the galaxy as a whole, such as the number and twist of the spiral arms. 

#### Map Layer Data

Below that is an editor for map layer data which allows the creation of a density map defining the value of some variable as it varies over the space containing the galaxy, which can be previewed at the top of the inspector. By default the star density map layer is displayed, which controls the distribution of stars. Any number of map layers can be created, defining variables such as the radius of zones and the presence of life and resources to be mined.

#### Star Tools

After the map layer data section is a foldout containing tools which allow you to generate stars according to the star density map. Stars are placed by accumulating density while walking over a space-filling Hilbert curve, maintaining some minimum distance between stars. This isn't as good as a proper sampling algorithm like Poisson disk sampling or Mitchell’s best-candidate algorithm, but it gets the job done ([please feel free to contribute a better sampling algorithm!](https://github.com/rwvens/Aetheria-Economy/issues/15)).

After generating stars, you can generate the links between them, which performs a Delaunay Tessellation, and then remove some proportion of links until the desired sparsity is reached. The algorithm for filtering star links is also not ideal, [there's an issue for fixing that, too!](https://github.com/rwvens/Aetheria-Economy/issues/25)

## Contact Us

If you want to chat, please join [our Discord server](https://discord.gg/trbteNj).