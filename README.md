# Aetheria-Economy
Economy Simulation for Aetheria



## Architecture

There are two solutions in this repository. One is a Unity project containing the desktop client for Aetheria, and the other is a .NET Core server application. There is some code shared between them, located at [Assets/Scripts/ServerShared](Assets/Scripts/ServerShared), defining a common protocol and data serialization format. Client-Server communication is implemented using [MagicOnion](https://github.com/Cysharp/MagicOnion), an RPC framework that transmits [MessagePack](https://github.com/neuecc/MessagePack-CSharp) over the wire.

Aetheria uses [RethinkDB](https://rethinkdb.com/) for data persistence. To make this possible, all persistent data is marked with attributes for both MessagePack and [JSON.Net](https://www.newtonsoft.com/json) serialization. During operation, the client does not communicate with the database server directly, only the game server does that; the game server caches data relevant to the game and sends it to the clients.

## Database Editor Tools

In order to facilitate the creation and maintenance of game data, there is a Unity editor utility which communicates directly with RethinkDB. You can access the tools by selecting Window/Aetheria Database Tools in Unity's menu. This will cause two windows to appear, the Database List View and the Database Inspector. 

##### Connecting to RethinkDB

At the top of the list view there is a text field where you can enter the URL of the database server. When you click connect, the editor will download and cache all of the items in the game, as well as subscribe to the changefeed. The list should now populate with items. For access to our database servers and therefore live game data, please contact us; it would be dangerous to make our actual database URL public!

##### Editing Items

You can unfold the categories of items in the list view to see what items exist. If you select an item, the Database Inspector will populate with all of the available fields of that item. Any changes you make in the Inspector will automatically be pushed to RethinkDB. If you've connected to the production database, this will update the stats of in-game items in real-time!

## Contact Us

If you want to chat, please join [our Discord server](https://discord.gg/trbteNj).