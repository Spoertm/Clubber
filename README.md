# Clubber
A Discord bot for the DD Pals server that takes care of DD score roles.

## Commands
Prefix is `+`.
### Info
`whyareyou`

<br/>

`help` - gets a list of commands, or info regarding a specific command.

#### Overloads
`help`

`help [command]`

<br/>
<br/>

`stats` - provides statistics from the leaderboard for users that are in this server and registered. `statsf` shows all the information available.

#### Aliases
`me`

`statsf`

`statsfull`

#### Overloads
`stats [me | @mention]`

`stats id [Discord ID]`

<br/>

### Database
`register` - obtains user from their leaderboard ID and adds them to the database. (Requires Manage Roles permission)

#### Overloads
`register [leaderboard ID] [name | @mention]`

`register id [leaderboard ID] [Discord ID]`

<br/>
<br/>

`remove` - removes a user from the database. (Requires Manage Roles permission)

#### Overloads
`remove [name | @mention]`

`remove id [Discord ID]`

<br/>

### Roles
`pb` - updates your own roles, or a specific user's if specified.

#### Overloads
`pb [name | @mention]`

`pb id [Discord ID]`
