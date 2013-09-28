var exitpolls = new Object();
exitpolls = {
    init: function() {
        $(".timeline").spin("large");
        exitpolls.getContacts();
        exitpolls.updateStatistics();

        $(document).bind('refreshStatsEvent', function() {
            exitpolls.updateStatistics();
        });
        $(document).bind('refreshContacts', function() {
            exitpolls.getContacts();
        });

        //setInterval(exitpolls.updateStatistics, 5000);
    },
    showContact: function(contact, isAlerta) {
        if (isAlerta) {
            $("#timeline-alertas").append($("#tpl-timeline-alertas").jqote(contact));
            $("#alerta-" + contact.CentroId).data("contact", contact);
            $("#timeline-alertas").spin(false);
        } else {
            if (contact.SecondsToCall <= 0) {
                $("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contact));
            } else {
                $("#timeline-pendiente").append($("#tpl-timeline-proximas").jqote(contact));
                $("#contacto-" + contact.CentroId + " .timer").tzineClock({
                    zeroCallback: function(timer) {
                        var item = $(timer).parents(".timelineItem");
                        //var contactExpired = exitpolls.createContactObject(item);
                        item.remove();
                        //$("#timeline-pendiente").append($("#tpl-timeline-pendientes").jqote(contactExpired));
                        $(document).trigger('refreshContacts');
                        $(document).trigger('refreshStatsEvent');

                    }
                });
            }
            $("#contacto-" + contact.CentroId).data("contact", contact);
            $("#timeline-pendiente").spin(false);

        }
    },
    getContacts: function() {
        $.getJSON('/ExitPolls/GetContacts', function(data) {
            $(".timeline").html('');
            var contact;
            var i;
            for (i = 0; i < data.proximos.length; i++) {
                contact = data.proximos[i];
                exitpolls.showContact(contact, false);
            }

            for (i = 0; i < data.alertas.length; i++) {
                contact = data.alertas[i];
                exitpolls.showContact(contact, true);
            }
            $(".timeline").spin(false);
        });

    },
    sortByTime: function(a, b) {
        return Number($(a).find(".timer").attr("seconds-to-call")) >
            Number($(b).find(".timer").attr("seconds-to-call")) ?
            1 : -1;
    },
    updateStatistics: function() {
        $.getJSON('/Participacion/GetStatistics', function(data) {
            $("#stats-centros").html(data.Centros);
            $("#stats-mesas").html(data.Mesas);
            $("#stats-participacion").html(data.Participacion);
            $("#stats-movilizacion").html(data.Movilizacion);
        });
    },
    createContactObject: function(item) {
        return item.data('contact');
        ;
    },
    updateExitPoll: function(item, contacto, values) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/ExitPolls/UpdateExitPoll",
            data: JSON.stringify({ contacto: contacto, valores: values }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });

    },
    updateSingleExitPoll: function(id, callback) {

        var valores = new Array();
        $(".input-" + id).each(function() {
            var cid = $(this).attr('candidato-id');
            var val = $(this).val();
            valores.push({ name: 'candidato-' + cid, value: val });
        });
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/ExitPolls/UpdateSingleExitPoll",
            data: JSON.stringify({ exitpollId: id, valores: valores }),
            cache: false,
            dataType: "json",
            success: function() {
                if ($.isFunction(callback)) {
                    callback();
                }
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });

    },
    updateLastParticipacion: function(item, contacto, value, cola) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/Participacion/UpdateLastParticipacion",
            data: JSON.stringify({ contacto: contacto, valor: value, cola: cola }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    alertaExitPoll: function(item, contacto, value, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/ExitPolls/AlertaExitPolls",
            data: JSON.stringify({ contacto: contacto, valor: value, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    updateAlertaExitPoll: function(item, contacto, mensaje) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/ExitPolls/ActualizarAlertaExitPolls",
            data: JSON.stringify({ contacto: contacto, mensaje: mensaje }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    },
    cancelAlertaExitPoll: function(item, contacto) {
        $.ajax({
            contentType: 'application/json, charset=utf-8',
            type: "POST",
            url: "/ExitPolls/CancelarAlertaExitPolls",
            data: JSON.stringify({ contacto: contacto }),
            cache: false,
            dataType: "json",
            success: function(contact) {
                $(document).trigger('refreshContacts');
                $(document).trigger('refreshStatsEvent');
            },
            error: function(x, ts, et) {
                alert('error ' + x + "-" + ts + "-" + et);
            }
        });
    }
};