var setup = new Object();
setup = {
    uploadFinished: false,
    init: function() {
        var url = arguments[0].url;
        var uploadCallback = arguments[0].uploadCallback;
        setup.initUploader(url, uploadCallback);
    },
    initUploader: function(url) {
        var uploader = new plupload.Uploader({
            runtimes: 'gears,html5,html4,flash,browserplus',
            browse_button: 'pickfiles',
            container: 'uploader',
            max_file_size: '10mb',
            url: url,
            flash_swf_url: '/plupload/js/plupload.flash.swf',
            filters: [
                { title: "Xlsx files", extensions: "xlsx" }
            ],
            resize: { width: 320, height: 240, quality: 90 }
        });
        uploader.bind('Init', function(up, params) {
            $('#filelist').html("<div>Por favor suba un archivo</div>");
        });
        $('#uploadfiles').click(function(e) {
            uploader.start();
            e.preventDefault();
        });
        uploader.init();
        uploader.bind('FilesAdded', function(up, files) {
            $.each(files, function(i, file) {
                $('#filelist').html(
                    "<div>Por favor suba un archivo con los nombres de sus usuarios </div>" +
                        '<div id="' + file.id + '">' +
                            file.name + ' (' + plupload.formatSize(file.size) + ') <b></b>' +
                                '</div>');
            });

            up.refresh(); // Reposition Flash/Silverlight
        });
        uploader.bind('UploadProgress', function(up, file) {
            $('#' + file.id + " b").html(file.percent + "%");
            $("#userFileList").hide();
            $("#respuesta").show();
            $("#respuesta").spin('large');
            $("#filePanelSpinner").show().spin("small");
        });
        uploader.bind('Error', function(up, err) {
            if (err.code == "-601") {
                alert("Archivo no valido. Debe usar un archivo en formato xlsx");
            } else {
                $('#filelist').append("<div>Error: " + err.code +
                    ", Message: " + err.message +
                    (err.file ? ", File: " + err.file.name : "") +
                    "</div>"
                );
            }
            up.refresh(); // Reposition Flash/Silverlight
        });
        uploader.bind('FileUploaded', function(up, file) {
            $('#' + file.id + " b").html("100% finished");
        });
    }
};