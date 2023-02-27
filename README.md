# EcoDataExporter
Currently Updated for use with Eco v0.9.X

A repo of the modules used by ElixrMods to export data from an ECO server for use on the Wiki. 
It currently exports the data required for the wiki modules in a lua table.

A number of lua table data files are exported for:
Items & World Object
Recipes
Animals
Plants
Trees
Skills
Talents
Commands

NO OTHER MODS should be used when exporting data for the wiki besides this one as it iterates through all loaded objects.

## Usage
In order to get the files use the command "/dumpdetails" in game, your in game player should find an area as flat as possible 
as while the code makes every effort to remove things in the way to get data (objects are physically placed to get some details from them)
there are occasions where something may prevent placement.

It is possible to produce Localized versions of the files by having your server language set appropriately, but the localizations are
reliant on the ECO localization project, if the strings there are not localized, neither will the files produced.

## Links
For a detailed document about this mod see: https://docs.google.com/document/d/1wMSBiHt4htns7pPiG5CJQDDzqHFxL8Pg_dn-h3BpDko/edit?usp=sharing

For use on the wiki come join the ECO Contribution Wiki Discord: https://discord.gg/N7tFc8J

Eco mod development kit: https://wiki.play.eco/en/Mod_Development

## Contributions
For further access to this project message Scotty(aka Zeel) in Discord: Scotty#7289
