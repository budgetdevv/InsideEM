# InsideEM
An asynchronous interactive Embed library for Discord.Net. Designed with efficiency and minimal memory footprints in mind.


# Notes

This lib is...

- `Work In Progress:` meaning its still under active development and you should only use it at your own risk ( It doesn't even build at time of writing )

- `Abuses the language:` lots of `Unsafe` code... :eyes: Some of which have yet to be tested for performance regressions.

- `Probably hard to use:` There's little or no abstraction at time of writing. However, this is something that will be improved on!

# Motivation

There seem to be little interative libs out there for Discord.Net. The goal here is to implement `a feature-rich, easy-to-use ( Working on that :p )` interative library, that may be used to build EmbedMenus!

# Concepts

___EmbedMenu___

An `EmbedMenu` would refer to an instance of an `Embed Message.` An `Embed Message` typically consists of `Title, Description ( Mandatory )` and `Fields`

___EmbedReactions___

An `EmbedReaction` would refer to a single `Embed Field`, which consists of `Title` and `Description`...both of which are mandatory. Typically, `EmbedReactions` contain `Actions`; think of those as `stored functions`. Such functions manipulate the Menu `E.x. Reacting with :eyes: would cause a new Menu to be created, destroying the old one...etc`

This library mainly revolves around this concept, which is highly customizable!

# Planned Features

- `Auto Expiry / Cancellation:` Set duration before the Menu closes itself automatically. This is to prevent memory leaks.

- `Auto Pagination:` As `Discord Embeds` are limited to `25 Fields,` lack of pagination would be problematic for huge amounts of `EmbedReactions!` Instead, they are automatically `paginated`, and you may configure the maximum allowed number of such Reactions per page.

- `Per-page Reactions:` Only `EmbedReactions` on the current page may be triggered! This facilitates the use of duplicate Reacts ( With the same emoji ), across multiple pages!

- `Reslient Fallback:` This lib attempts to edit `EmbedMessages` in place of `deleting and re-creating,` which is a horrible UX if you ask me. However, such is not always possible due to the bot's inability to remove `user-added Reactions` ( No Manage Messages perms / Messages in DMs ). The lib fallsback to deleting and re-creating messages when editing isn't possible, and vice-versa!

- `Nav Buttons:` Built-in `navigational buttons` for `returning to previous EmbedMenu`, `pagination buttons`, `cancel button` etc...

- `Image, Thumbnail, Footer support:` This lib provides __FULL__ API coverage for editing parts of an `Embed.`

- `Anti-duplication:` Prevent users from lanuching multiple instances of EmbedMenus! This restriction may be `lifted.`

- `Thread-safe out of the box:` The lib aims to be `thread-safe!`

- `Minimal overhead / memory footprints:` As mentioned above, this lib aims to add as little overhead as possible! It does so by using Unsafe code, and recycling memory.
