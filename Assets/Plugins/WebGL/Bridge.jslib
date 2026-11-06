mergeInto(LibraryManager.library, 
{
    SendStringToPage: function (str)
    {
        var managedString = UTF8ToString(str);
        console.log("Recieved message from Unity: " + managedString);

        if(window.onUntyMessageRecieved)
            window.onUntyMessageRecieved(managedString);
    }

});