function deleteFile(fileName, postHandler) {
    if (confirm("Are you sure you want to delete?") == true) {
        $.post(postHandler + "?mode=delete", {
            fileName: fileName,
        })
        .success(function (data) {
            location.reload();
        })
        .fail(function (data) {
            $("#statusMessage").html("Something bad happened. Server reported " + data.status + " " + data.statusText);
            setTimeout(function () {
                $("#statusMessage").html("");
            }, 4000);
        });
    }
}

function uploadFile(postHandler) {
    var data = new FormData();
    jQuery.each(jQuery('#file')[0].files, function (i, file) {
        data.append('file-' + i, file);
    });

    $.ajax({
        type: "POST",
        url: postHandler + "?mode=file",
        cache: false,
        contentType: false,
        processData: false,
        data: data
    })
    .success(function (data) {
        $("#statusMessage").html("The file was saved successfully");
        setTimeout(function () {
            location.reload();
        }, 1000);
    })
    .fail(function (data) {
        if (data.status === 409) {
            $("#statusMessage").html("The filename is already in use");
        } else {
            $("#statusMessage").html("Something bad happened. Server reported " + data.status + " " + data.statusText);
        }
        setTimeout(function () {
            $("#statusMessage").html("");
        }, 4000);
    });
}

function saveFile(postHandler) {
    var postData = new FormData();
    var lines = $("textarea[name='lines']").val();
    postData.append('lines', lines);
    postData.append('fileName', $('#fileName').val());

    $.ajax({
        type: "POST",
        url: postHandler + "?mode=save",
        cache: false,
        contentType: false,
        processData: false,
        data: postData
    })
    .success(function (data) {
        $("#statusMessage").html("The file was saved successfully");
        setTimeout(function () {
            location.reload();
        }, 1000);
    })
    .fail(function (data) {
        if (data.status === 409) {
            $("#statusMessage").html("The filename is already in use");
        } else {
            $("#statusMessage").html("Something bad happened. Server reported " + data.status + " " + data.statusText);
        }
        setTimeout(function () {
            $("#statusMessage").html("");
        }, 4000);
    });
}

function createFile(postHandler) {
    var postData = new FormData();
    postData.append('fileName', $('#fileName').val());

    $.ajax({
        type: "POST",
        url: postHandler + "?mode=create",
        cache: false,
        contentType: false,
        processData: false,
        data: postData
    })
    .success(function (data) {
        $("#statusMessage").html("The file was saved successfully");
        setTimeout(function () {
            location.reload();
        }, 1000);
    })
    .fail(function (data) {
        if (data.status === 409) {
            $("#statusMessage").html("The filename is already in use");
        } else {
            $("#statusMessage").html("Something bad happened. Server reported " + data.status + " " + data.statusText);
        }
        setTimeout(function () {
            $("#statusMessage").html("");
        }, 4000);
    });
}