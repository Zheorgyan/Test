$("body").on('click', '.karmanItem', function (e) {
    e.preventDefault();
    var href = $(this).attr('href');
    AJAXGlobals({
        url: href,

        data: {
            _mnemoName: $(this).data('plainMnemo'),
            _stackName: $(this).data('plainStack'),
            _pocketNum: $(this).data('plainPocket')
        },
        success: function (data) {

            $('#dialogContent').html(data.resultHtml);

            $('#modDialog').modal('show');
            
        }
    });
});

$("body").on('click','.sortItem',function(e) {
    e.preventDefault();
    var href = $(this).attr('href');
    AJAXGlobals({
        url: href,
        data: {
            _mnemoName: $(this).data('plainMnemo'),
            _stackName: $(this).data('plainStack'),
            _pocketNum: $(this).data('plainPocket'),
            _diameter: $(this).data('plainDiam'),
            _thickness: $(this).data('plainThick'),
            _stal: $(this).data('plainStal'),
            _gost: $(this).data('plainGost')
        },
        success: function(data) {
            $('#dialogContent2').html(data.resultHtml);

            $('#modDialog2').modal('show');

        }
    });
});