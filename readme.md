# Library

## Description

library "Library" containce methods:
1) Load/Save data to specific file in XML format
2) Add/Update book to library
3) Get and search books with ordering priorities (author name, title)

library realized as tree of the books and authors which has references to specific files

data base made as filesystem and as default every file broken into 5-8 MB with index file to save references
and realized cache imitation by hash table whith limit 200 items to decrease cost for update book

## Known Issues

search can dont show every books if some of them on other file 

