CONTENTS OF THIS FILE
---------------------
   
 * Introduction
 * Requirements
 * Installation
 * Configuration
 * Troubleshooting
 * FAQ
 * Maintainers

----------------------------
-------Introduction-------
----------------------------
This project was made by Yakir Herskovitz & Shahar Cohen as part of
Information Retrieval course in Ben Gurion university.
The engine can process corpus of data and analyze the words in it. the final goal
of this is to process queries from users and retrieving the relevants documents

--------------------------
------Installation-------
--------------------------
Just extract the project from the rar file to a folder and than run the
.exe file

--------------------------
-----Configuration------
--------------------------
In the main window you have to choose the path of the corpus, and the path of the
destinated posting files. be aware that in the corpus folder you have to include
"stopWords.txt"! in order for the program to work flawless.
after you finished the configurations you just have to hit the button "Build index".
It may take few minutes for the program to run so be patient.

-----------------------------
--Troubleshooting&FAQ--
-----------------------------
Q: I pressed build index and now the program is running for a long time. how do i know if the program is stuck?
A: If you run the program through Visual Studio you can watch the output window. every doc that is sent to the indexer 
is written there before. that way you can keep track on it. If you using the .exe version you need to wait until you
see the popup message that notify you when the process is done.
*in the next versions we hope to have a progress bar in the GUI
Q: I want to run this program in Visual Studio what do i have to do?
A: You need to make sure that you have the required assemblies. right click on refernces folder in the solution explorer window, 
than press on add reference. go to assemblies -> frameworks and search in the search bar for Systems.Windows.Forms and check it.
You will need to download also HtmlAgilityPack. downloading it from Visual Studio is relatively easy. just go the top right corner to the search bar.
type in "Nuget project" and there go to the Browse tab and search for "HtmlAgilityPack". download the first result there and now you successfully
installed it and ready to run our program
Q:Why HtmlAgilityPack?
A:Because we don't have to reinvent the wheel. Someone already made a library that takes care about Html tags. 
It's far better implemented that what we can do, so we figured, why not to use it.

-------------------------
------Maintainers------
-------------------------
Yakir Herskovitz - https://github.com/yakirhe
Shahar Cohen - https://github.com/shahar3