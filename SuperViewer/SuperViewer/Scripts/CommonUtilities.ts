module CommonUtilities {

    export var getQueryStringParameterByName = (name: string) => {
        name = name.replace(/[\[]/, '\\[').replace(/[\]]/, '\\]');
        var regex = new RegExp('[\\?&]' + name + '=([^&#]*)', 'i'),
            results = regex.exec(location.search);
        return results === null ? '' : decodeURIComponent(results[1].replace(/\+/g, ' '));
    }

    export var getStringFromDate = (jsDate: Date) => {
        var day = jsDate.getDate();        // yields day
        var month = jsDate.getMonth() + 1;    // yields month
        var year = jsDate.getFullYear();  // yields year
        var hour = jsDate.getHours();     // yields hours 
        var minute = jsDate.getMinutes(); // yields minutes
        var second = jsDate.getSeconds(); // yields seconds        
        return month + "/" + day + "/" + year + " " + hour + ":" + minute + ":" + second;
    }
}