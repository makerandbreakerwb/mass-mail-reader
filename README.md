# Mass mail reader

Useful when you have directory of .elm email files and you want to have them searchable.



* Fist call `.\MassMailReader.exe read --directory ??? --parallel` to index your directory of mails.
    * You have to do this just once.
    * This creates `mails.sqlite` file in given directory. It's sqlite database with single table.
    * Database contains mail path, content, subject and all attachments.
* After indexing, you can call  `.\MassMailReader.exe search --directory ???`.
    * After you'll be promoted for query. This searches in subject and content of mails.
    * Maximum of 250 is returned
    * Alternatively you can use any sqlite client (ie. SQLiteStudio) to query the database directly.
