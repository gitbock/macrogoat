
$(document).ready(function () {

    //creates html table with result of API response
    // takes raw json and columns which should be displayed in table later.
    // columns is like a filter for the raw json object
    function populateResultTableFromJson(jsonResult, columns) {
        //only if data is present / not null
        var displayObject = new Object();
        for (var i = 0; i < columns.length; i++) {
            var value = columns[i];
            if (jsonResult[value] != null && jsonResult[value].length > 0) {
                // add to display object to be shown later.  ~ filtered object
                displayObject[value] = jsonResult[value];
            }
        };

        // create result table
        var res = '<table class="table border-left-success"><tbody>';
        for (var prop in displayObject) {

            if (prop.toLowerCase().indexOf("url") !== -1) {
                //contains URL -> make <a>
                res += '<tr>' +
                    '<td><span class="font-weight-bold">' + prop + '</span></td>' +
                    '<td><a href="' + displayObject[prop] + '"><span class="text-break">' + displayObject[prop] + '</a></span></td>' +
                    '</tr>';
            }
            else {
                res += '<tr>' +
                    '<td><span class="font-weight-bold">' + prop + '</span></td>' +
                    '<td><span class="text-break">' + displayObject[prop] + '</span></td>' +
                    '</tr>';
            }
            
        };
        res += '</tbody></table>';
        return res;
    }

    // Posting Verify Form to API
    $('#verifyform').submit(function myfunction(e) {
        // not submitting form by Browser
        e.preventDefault();

        //get url for ajax from page
        var form = $(this);
        var fdataWithFiles = new FormData(form[0]);
        var ApiUrl = form.attr('action');

        $.ajax({
            type: "POST",
            url: ApiUrl,
            contentType: false,
            processData: false,
            data: fdataWithFiles,
            success: function (data) {
                var jsonResult = $.parseJSON(data);
                console.log(jsonResult);
                if (jsonResult.Status === "ready") {
                    $('#statusicon').html("<a href='#' class='btn btn-success btn-circle btn-lg'> <i class='fas fa-check-circle'></i></a>");
                    $('#statusmessage').addClass("text-success font-weight-bold").removeClass("text-danger");
                    var htmltable = populateResultTableFromJson(jsonResult, ["FileHash", "CertIssuedTo", "CertIssuedBy", "CertHash", "CertExpire"]);
                    $('#apiresultrow').html(htmltable);

                }
                else {
                    $('#statusicon').html("<a href='#' class='btn btn-danger btn-circle btn-lg'> <i class='fas fa-exclamation-triangle'></i></a>");
                    $('#statusmessage').addClass("text-danger font-weight-bold").removeClass("text-success");
                    // clear old output if any
                    $('#apiresultrow').html("");
                }
                //Last Message showing
                $('#statusmessage').html(jsonResult.Message);


            }
        });
        
    });


   

    // Updates message and icon in result table while polling for new status
    // if true is returned it's the signal for pollFunc to stop!
    // callback is used to signal end of function to caller / poller function
    function updateResultFromUrl(statusApiUrl, callback) {
        //fetch Status URL from data attribute
        $.ajax({
            type: "GET",
            url: statusApiUrl,
            success: function (data) {
                var jsonResult = $.parseJSON(data);
                console.log(jsonResult);
                if (jsonResult.Status === "ready") {
                    // no need to execute further
                    console.log("updated returned ready");
                    $('#statusmessage').addClass("text-success font-weight-bold").removeClass("text-primary text-danger");
                    $('#statusicon').html("<a href='#' class='btn btn-success btn-circle btn-lg'> <i class='fas fa-check-circle'></i></a>");
                    $('#apiresultrow').html(populateResultTableFromJson(jsonResult, ["FileHash", "CertIssuedTo", "CertIssuedBy", "CertHash", "CertExpire", "DownloadUrl"]));
                    callback(true);
                }
                else if (jsonResult.Status === "error") {
                    // stop on error
                    console.log("updated returned error. Stopp.");
                    $('#statusmessage').addClass("text-danger font-weight-bold").removeClass("text-success text-primary");
                    $('#statusicon').html("<a href='#' class='btn btn-danger btn-circle btn-lg'> <i class='fas fa-exclamation-triangle'></i></a>");
                    callback(true);
                }
                else {
                    //still in progress -> update Status
                    $('#statusmessage').addClass("text-primary font-weight-bold").removeClass("text-success text-danger");
                    //$('#statusicon').html("<div class='spinner-grow text-primary'> </div>");
                    console.log("updated returned not ready yet");
                    callback(false);
                }
                $('#statusmessage').html(jsonResult.Message);

            },
            error: function (data) {
                console.log("Status Update failed.");
                callback(true);
            }
        });

    }


    // calls function "fn" with parameters "param" for specified interval
    // called function must use calls callback function to update stop signal
    function pollFunc(fn, timeout, interval, param) {
        var startTime = (new Date()).getTime();
        interval = interval || 1000;
        canPoll = true;
        var updateEnded;
        (function p() {
            canPoll = ((new Date).getTime() - startTime) <= timeout;
            
            fn(param, function (updateEndedResultFromFunction) {
                updateEnded = updateEndedResultFromFunction;
            });
            //console.log("Update=" + updateEnded+ " Timeout: " + ((new Date).getTime() - startTime));
            if (!canPoll) {
                console.log("Timeout occured!");
            }
            if (!updateEnded && canPoll) { // ensures the function exucutes until timeout or stopp signial
                setTimeout(p, interval, param);
            }
        })();
    }


    // submitting files to API for analysing / signing
    $('#adhocsignerform').submit(function myfunction(e) {
        // not submitting form by Browser
        e.preventDefault();

        //get url for ajax from page
        var form = $(this);
        var fdataWithFiles = new FormData(form[0]);
        var ApiUrl = form.attr('action');

        //only execute api request if minimum inputs are set
        if (document.querySelector("#officefileselect").files.length == 0 || document.querySelector("#certfileselect").files.length == 0) {
            $('#statusicon').html("<a href='#' class='btn btn-danger btn-circle btn-lg'> <i class='fas fa-exclamation-triangle'></i></a>");
            $('#statusmessage').addClass("text-danger font-weight-bold").removeClass("text-success");
            $('#statusmessage').html("You must select a office file and cert file!");
            return;
        }
        else {
            // clear from last run 
            $('#statusmessage').html("");
            $('#statusicon').html("");
        }


        $.ajax({
            type: "POST",
            url: ApiUrl,
            contentType: false,
            processData: false,
            data: fdataWithFiles,
            success: function (data) {
                var jsonResult = $.parseJSON(data);
                console.log(jsonResult);

                if (jsonResult.Status.startsWith("queued")) {
                    //start inprogress icon
                    $('#statusicon').html("<div class='spinner-grow text-primary'> </div>");
                    $('#statusmessage').html("<div class='text-primary'>In Progres...</div>");

                    //Start Api Status Monitor for updating result table until API thread is finished with "ready"
                    pollFunc(updateResultFromUrl, 40000, 2000, jsonResult.StatusUrl);
                }
                else {
                    //error while posting
                    $('#statusicon').html("<a href='#' class='btn btn-danger btn-circle btn-lg'> <i class='fas fa-exclamation-triangle'></i></a>");
                    $('#statusmessage').addClass("text-danger font-weight-bold").removeClass("text-success");
                    $('#statusmessage').html(jsonResult.Message);
                }
                
            },
            error: function (jqxhr, status, exception) {
                alert('Exception:', exception);
            }
        });



    });


    

})



