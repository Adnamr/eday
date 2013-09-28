    var participacionChart;
    var participacionMovilizacionChart;
    var participacionChart;
    var alertasChart;
    var alertasActivasChart;
    var alertasPorEstadoChart;
    var totalizacionChart;
    var quickcountChart;

    function pad2(number) {

        return (number < 10 ? '0' : '') + number;

    }

    function MinimosCuadrados(pctParticipacion, utcDate) {
        var n = pctParticipacion.length;
        var A = 0;
        var B = 0;
        var C = 0;
        var D = 0;


        for (var i = 0; i < n; i++) {
            A = A + pctParticipacion[i][0];
            B = B + pctParticipacion[i][1];
            C = C + (pctParticipacion[i][0] * pctParticipacion[i][0]);
            D = D + (pctParticipacion[i][0] * pctParticipacion[i][1]);
        }

        var m = (n * D - A * B) / (n * C - (A * A));
        var b = (C * B - D * A) / (n * C - (A * A));

        return m * utcDate + b;

    }
    function thousandSeparator(n, sep) {
        var sRegExp = new RegExp('(-?[0-9]+)([0-9]{3})'),
	sValue = n + '';

        if (sep === undefined) { sep = ','; }
        while (sRegExp.test(sValue)) {
            sValue = sValue.replace(sRegExp, '$1' + sep + '$2');
        }
        return sValue;
    }

    function updateIndicadores() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetIndicadores',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {

                $("#stats-centros").html(thousandSeparator(data.muestra.centros, '.'));
                $("#stats-mesas").html(thousandSeparator(data.muestra.mesas, '.'));
                $("#stats-votantes-rep").html(thousandSeparator(data.muestra.rep, '.'));
                $("#stats-enlaces").html(thousandSeparator(data.muestra.enlaces, '.'));

                $("#stats-centros-participacion").html(thousandSeparator(data.generales.centros, '.') + "/" + data.generales.pctCentros.toFixed(2) + "%");
                $("#stats-mesas-participacion").html(thousandSeparator(data.generales.mesas, '.') + "/" + data.generales.pctMesas.toFixed(2) + "%");
                $("#stats-votates-rep-mesas").html(thousandSeparator(data.generales.repmesas, '.'));
                $("#stats-votantes-rep-centros").html(thousandSeparator(data.generales.repcentros, '.'));
                //$("#stats-participacion-cargas-por-hora").html(data.generales.contactosPorHoraPorUsuario.toFixed(2));

                $("#stats-alarmas-total").html(thousandSeparator(data.alarmas.alertasCount, '.'));
                $("#stats-alarmas-ultima-hora").html(thousandSeparator(data.alarmas.alertaCountLastHour, '.'));
                $("#stats-alarmas-enlaces").html(thousandSeparator(data.alarmas.enlacesWithAlerta, '.') + "/" + data.alarmas.pctEnlacesAlerta.toFixed(2) + "%");
                $("#stats-alarmas-enlace-promedio").html(data.alarmas.avgAlarmaPorEnlace.toFixed(2));
                $("#stats-alarmas-centro-promedio").html(data.alarmas.avgAlarmaPorCentro.toFixed(2));

                $("#stats-participacion-votantes").html(thousandSeparator(data.participacion.votantesMesa, '.'));
                $("#stats-participacion-mesas-part-vs-rep").html(data.participacion.pctMesa.toFixed(2) + "%");
                $("#stats-participacion-proyeccion").html(thousandSeparator(data.participacion.votantesCentro, '.'));
                $("#stats-participacion-centros-part-vs-rep").html(data.participacion.pctCentro.toFixed(2) + "%");
                //$("#stats-participacion-proyeccion-total").html("N/A");


                $("#stats-movilizacion-centros").html(thousandSeparator(data.movilizacion.centro, '.'));
                $("#stats-movilizacion-REP").html(thousandSeparator(data.movilizacion.repMovilizado, '.') + "<br/>"
                            + (data.movilizacion.repMovilizado * 100.0 / data.movilizacion.repTotal).toFixed(2) + "%");
                $("#stats-movilizacion-votantes").html(thousandSeparator(data.movilizacion.conteo, '.'));
                $("#stats-movilizacion-pct").html((data.movilizacion.conteo * 100.0 / data.movilizacion.repMovilizado).toFixed(2) + "%");


                $("#stats-quickcount-centros").html(thousandSeparator(data.totalizacion.qcCerrados, '.'));
                $("#stats-quickcount-pct").html((data.totalizacion.qcCerrados * 100.0 / data.totalizacion.centrosTotales).toFixed(2) + "%");
                $("#stats-totalizacion-mesas").html(thousandSeparator(data.totalizacion.mesasTotalizadas, '.'));
                $("#stats-totalizacion-pct").html((data.totalizacion.mesasTotalizadas * 100.0 / data.totalizacion.mesas).toFixed(2) + "%");

            }
        });
    }
    function updateAlertasPorTendencia() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetAlertasActivasPorTendencia',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {
                var alertas = new Array();
                var item = -1;
                var currentAlert = "";
                var o = {};
                for (var i = 0; i < data.length; i++) {
                    if (currentAlert != data[i].name) {
                        if (item >= 0) {
                            var total = Number(o.neutro) + Number(o.oposicion) + Number(o.chavista);
                            if (total > 0) {
                                o.total = total;
                                o.oposicionPct = (o.oposicion * 100.0 / total).toFixed(2);
                                o.chavistaPct = (o.chavista * 100.0 / total).toFixed(2);
                                o.neuttroPct = (o.neutro * 100.0 / total).toFixed(2);

                                alertas.push(o);
                            }

                        }
                        item++;
                        currentAlert = data[i].name;
                        o = {};
                        o.name = currentAlert;
                    }

                    if (data[i].tag1 == "Chavista")
                        o.chavista = data[i].ocurrencias;
                    if (data[i].tag1 == "Oposicion")
                        o.oposicion = data[i].ocurrencias;
                    if (data[i].tag1 == "Neutro")
                        o.neutro = data[i].ocurrencias;
                }

                $("#alertasTendenciaTable tbody").html($("#tplAlertaTendencia").jqote(alertas));

            }
        });
    }
    function updateTotalizacionPorTendencia() {
        $.ajax({
            url: '/Dashboard/GetTotalizacionPorTendencia',
            dataType: 'json',
            type: 'POST',
            success: function (data) {
                console.log(data);
                for (var i = 0; i < data.length; i++ ) {
                    data[i].pct = (data[i].totalizados*100.0/data[i].total).toFixed(2);
                }
                $("#totalizacionTendenciaTable tbody").html($("#tplTotalizacionTendencia").jqote(data));

                
            }
        });
    }
    
    function parseDate(dt) {

        var pieces = dt.split("T");
        var fecha = pieces[0];
        var horaObj = pieces[1].split(".")[0];
        var fechaParts = fecha.split("-");
        var year = fechaParts[0];
        var month = fechaParts[1];
        var day = fechaParts[2];
        var hourParts = horaObj.split(":");
        var hora = hourParts[0];
        var min = hourParts[1];
        var sec = 0;

        return new Date(year, month, day, hora, min, sec);
        
    }

    function updateParticipacionMovilizacion()

    {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetParticipacionMovilizacionData',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {

                var pctParticipacion = new Array();
                var proyeccionParticipacion = new Array();
                var pctMovilizacion = new Array();
                var pctPartOverMov = new Array();
                for (var i = 0; i < data.length; i++) {
                    var parts = data[i].fecha.split("T");
                    var hour = parts[1];
                    var hourParts = hour.split(":");


                    //var now = new Date(data[i].fecha);
                    var now = new Date(2013,4-1, 14,hourParts[0],hourParts[1]);

                    var utc = Date.UTC(now.getFullYear(), now.getMonth(), now.getDate(), now.getHours(), now.getMinutes());

                    pctParticipacion.push([utc, data[i].pctParticipacion]);
                    pctMovilizacion.push([utc, data[i].pctMovilizacion]);
                    data[i].pctParticipacion = data[i].pctParticipacion.toFixed(2);
                    data[i].pctMovilizacion = data[i].pctMovilizacion.toFixed(2);
                    if (data[i].pctParticipacion != 0)
                        pctPartOverMov.push([utc, data[i].relParticipacionMovilizacion]);
                    else
                        pctPartOverMov.push([utc, 100]);

                    data[i].strDate =
                        now.getDate() + "/" + (now.getMonth() + 1) + "/" + now.getFullYear() + " " +
                        pad2(now.getHours()) + ":" + pad2(now.getMinutes());
                }

                $("#participacionTable tbody").html(
                                $("#tplParticipacionRow").jqote(data.reverse()));
                var today = new Date();
                var startDate = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate(), 6, 0, 0);
                var endDate = Date.UTC(today.getFullYear(), today.getMonth(), today.getDate(), 18, 0, 0);

                var proyeccion = 0;
                if (pctParticipacion.length > 0) {

                    //Inicio la proyeccion en el primer punto muestral
                    startDate = pctParticipacion[0][0];
                    //Regresion de minimos cuadrados para el punto de proyeccion
                    proyeccion = MinimosCuadrados(pctParticipacion, endDate);
                    $("#stats-participacion-proyeccion-total").html(proyeccion.toFixed(2) + "%");
                    $("#stats-participacion").html(pctParticipacion[pctParticipacion.length - 1][1].toFixed(2) + "%");

                } else {
                    $("#stats-proyeccion-participacion").html("N/A");
                    $("#stats-participacion").html("N/A");
                }

                if (pctMovilizacion.length > 0) {
                    $("#stats-movilizacion").html(pctMovilizacion[pctMovilizacion.length - 1][1].toFixed(2) + "%");
                } else {
                    $("#stats-movilizacion").html("N/A");
                }
                proyeccionParticipacion.push([startDate, 0]);
                proyeccionParticipacion.push([endDate, proyeccion]);

                participacionChart.series[0].setData(pctParticipacion);
                participacionChart.series[1].setData(pctMovilizacion);
                participacionChart.series[2].setData(proyeccionParticipacion);
                participacionChart.series[3].setData(pctPartOverMov);
            }
        });
    }
    function updateAlertas() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetAlertas',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {

                var totalActivas = 0;
                var totalAcumulado = 0;
                var series;
                var i;
                alertasChart.counters.color = 0;
                alertasActivasChart.counters.color = 0;
                alertasPorEstadoChart.counters.color = 0;
                while (alertasChart.series.length > 0) {
                    alertasChart.series[0].remove(false);
                }
                for (i = 0; i < data.totales.length; i++) {
                    series = {
                        name: data.totales[i].name,
                        type: 'bar',
                        data: []
                    };
                    if (Number(data.totales[i].ocurrencias)) { } else {
                        data.totales[i].ocurrencias = 0;
                        data.totales[i].centrosAfectados = 0;
                    }
                    totalAcumulado += data.totales[i].ocurrencias;
                    series.data.push([data.totales[i].name, data.totales[i].ocurrencias]);
                    alertasChart.addSeries(series, false);
                }

                while (alertasActivasChart.series.length > 0) {
                    alertasActivasChart.series[0].remove(false);
                }
                for (i = 0; i < data.activas.length; i++) {
                    series = {
                        name: data.activas[i].name,
                        type: 'bar',
                        data: []
                    };
                    if (Number(data.activas[i].ocurrencias)) { } else {
                        data.activas[i].ocurrencias = 0;
                        data.activas[i].centrosAfectados = 0;
                    }
                    totalActivas += data.activas[i].ocurrencias;

                    series.data.push([data.activas[i].name, data.activas[i].ocurrencias]);
                    alertasActivasChart.addSeries(series, false);
                }

                while (alertasPorEstadoChart.series.length > 0) {
                    alertasPorEstadoChart.series[0].remove(false);
                }
                for (i = 0; i < data.estadosl.length; i++) {
                    series = {
                        name: data.estadosl[i].estado,
                        type: 'column',
                        data: []
                    };
                    if (Number(data.estadosl[i].count)) { } else {
                        data.estadosl[i].count = 0;
                        data.estadosl[i].centrosAfectados = 0;
                    }
                    series.data.push([data.estadosl[i].estado, data.estadosl[i].count]);
                    alertasPorEstadoChart.addSeries(series, false);
                }

                alertasPorEstadoChart.redraw();
                alertasActivasChart.redraw();
                alertasChart.redraw();
                //                for (i = 0; i < data.totales.length; i++) {
                //                    data.totales[i].pct = (data.totales[i].ocurrencias * 100 / totalAcumulado).toFixed(2);
                //                }
                //                for (i = 0; i < data.activas.length; i++) {
                //                    data.activas[i].pct = (data.activas[i].ocurrencias * 100 / totalActivas).toFixed(2);
                //                }

                $("#alertasActivasTable tbody").html($("#tplAlertaRow").jqote(data.activas));
                $("#alertasAcumuladasTable tbody").html($("#tplAlertaRow").jqote(data.totales));
            }
        });
    }
    function updateMatrizCalique() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetMatrizCalique',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {
                var total = 0;
                var totalOpo = 0;
                var totalNini = 0;
                var totalChav = 0;
                var i;
                for (i = 0; i < data.matriz.length; i++) {
                    if (data.matriz[i].tag1 != '')
                        total += data.matriz[i].conteo;
                    if (data.matriz[i].tag1 == 'Oposicion') {
                        totalOpo += data.matriz[i].conteo;
                    }
                    if (data.matriz[i].tag1 == 'Neutro') {
                        totalNini += data.matriz[i].conteo;
                    }
                    if (data.matriz[i].tag1 == 'Chavista') {
                        totalChav += data.matriz[i].conteo;
                    }
                }

                $("#OposicionTotal").html(thousandSeparator(totalOpo, '.') + "/" + (totalOpo * 100.0 / total).toFixed(2) + "%");
                $("#NeutroTotal").html(thousandSeparator(totalNini, '.') + "/" + (totalNini * 100.0 / total).toFixed(2) + "%");
                $("#ChavistaTotal").html(thousandSeparator(totalChav, '.') + "/" + (totalChav * 100.0 / total).toFixed(2) + "%");
                for (i = 0; i < data.matriz.length; i++) {
                    $("#" + data.matriz[i].tag1 + data.matriz[i].tag2).html(thousandSeparator(data.matriz[i].conteo, '.') + "  / "
                        + (data.matriz[i].conteo * 100 / total).toFixed(2) + "%");
                }
            }
        });
    }
    function updateTotalizacion() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join(),
            quickCountOnly: false
        };

        $.ajax({
            url: '/Totalizacion/GetChartData',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {
                $("#totalizacionTable tbody").html('');
                $("#totalizacionTable tbody").append($("#tplCandidatoRow").jqote(data));
                var seriesData = new Array();
                for (var i = 0; i < data.length; i++) {
                    seriesData.push({ name: data[i].Nombre, y: data[i].Proyeccion, color: '#' + data[i].Color });
                }
                totalizacionChart.series[0].setData(seriesData, true);
                totalizacionChart.series[0].color = '#';
            }
        });
    }
    function updateQuickCount() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join(),
            quickCountOnly: true
        };
        $.ajax({
            url: '/Dashboard/GetQuickCountPorMuestras',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (datas) {
                //console.log(data);
                var data = datas.results;
                var tableData = datas.avances;


                $("#quickcountTable tbody").html($("#tplQuickCountRow").jqote(tableData));

                while (quickcountChart.series.length > 0) {
                    quickcountChart.series[0].remove(false);
                }

                var xaxis = new Array();
                for (var jj = 0; jj < data.length; jj++) {
                    xaxis.push(data[jj].Key);
                }
                var candidatoCount = data[0].Value.length;
                for (var i = 0; i < candidatoCount; i++) {

                    series = {
                        name: data[0].Value[i].nombre,
                        type: 'bar',
                        color: '#' + data[0].Value[i].color,
                        data: []
                    };
                    for (var j = 0; j < data.length; j++) {
                        if (data[j].Value[i].proyeccion == null) {
                            data[j].Value[i].proyeccion = 0;
                        }
                        series.data.push([data[j].Key, data[j].Value[i].proyeccion]);
                    }
                    quickcountChart.addSeries(series, false);
                }
                quickcountChart.xAxis[0].setCategories(xaxis);
                quickcountChart.redraw();
            }
        });

    }
    function updateSustitucion() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetMatrizSustitucion',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {
                $("#susticionTable tbody").html($("#tplSustitucionRow").jqote(data));
            }
        });
    }

    function updateProyeccionHistoricos() {
        var object = {
            tag1: $("#tag1").val() == null ? "" : $("#tag1").val().join(),
            tag2: $("#tag2").val() == null ? "" : $("#tag2").val().join(),
            estados: $("#estado").val() == null ? "" : $("#estado").val().join(),
            municipios: $("#municipio").val() == null ? "" : $("#municipio").val().join()
        };
        $.ajax({
            url: '/Dashboard/GetProyeccionPorParticipacion',
            dataType: 'json',
            data: object,
            type: 'POST',
            success: function (data) {

                $("#proyeccionParticipacion").html($("#tplHistoricoChart").jqote(data.muestras));
                _.each(data.muestras, function (item) {
                    
                        
                    //TODO: Cambiar esto por un find() cuando pueda leer la documentacion
                    _.each(data.global, function (gItem) {
                        if (gItem.id_muestra == item.id_muestra) {

                            var pctCapr = gItem.capr * 100.0 / (gItem.capr + gItem.chav + gItem.otro);
                            var pctChav = gItem.chav * 100.0 / (gItem.capr + gItem.chav + gItem.otro);
                            if (gItem.capr > gItem.chav) {


                                var margen = (pctCapr - pctChav).toFixed(2);
                                $("#ganador-" + item.id_muestra).html(" Capriles ganador con un margen de " + margen + "pp").addClass("success");
                    
                            }else{
                                var margen = (pctChav - pctCapr).toFixed(2);
                                $("#ganador-" + item.id_muestra).html(" Maduro ganador con un margen de " + margen + "pp").addClass("error");
                    
                            }
                            var a = new Highcharts.Chart({
                                chart: { renderTo: 'proyeccionGlobalHistorico-' + item.id_muestra },
                                credits: { text: "Generado por EdayRoom" },
                                title: { text: null, visible: false },
                                plotOptions: {
                                    pie: {
                                        allowPointSelect: true,
                                        cursor: 'pointer',
                                        dataLabels: {
                                            enabled: false
                                        },
                                        showInLegend: true
                                    }
                                },
                                tooltip: {
                                    formatter: function () {
                                        return '<b>' + this.point.name + '</b>: ' + this.percentage.toFixed(2) + ' %';
                                    }
                                },
                                series: [{
                                    type: 'pie',
                                    name: 'zup?',

                                    data: [{ name: 'Maduro', y: gItem.chav, color: '#f00' }, {name:'Capriles', y: gItem.capr, color:'yellow'},
                                        ['Otro', gItem.otro], ['Abstencion', gItem.abstencion]
                                    ]
                                }]
                            });
                        }
                    });
                    var regiones = new Array();
                    _.each(data.regional, function (rItem) {
                       
                        if (rItem.id_muestra == item.id_muestra) {
                            regiones.push(rItem);
                           
                        }
                    });
                    $("#proyeccionEstadoHistorico-" + item.id_muestra + " table tbody").html($("#tplHistoricoChartRow").jqote(regiones));

                });
                $(".mws-datatable").dataTable();
/*                
                _.map(data, function (item) {
                    item.totalCapriles = Math.round(item.totalCapriles);
                    item.totalChavismo = Math.round(item.totalChavismo);
                    item.pctCapriles = item.pctCapriles.toFixed(2);
                    item.pctChavismo = item.pctChavismo.toFixed(2);

                    var a = new Highcharts.Chart({
                        chart: { renderTo: 'proyeccionHistorico-' + item.id_muestra },
                        credits: { text: "Generado por EdayRoom" },
                        title: {text: null, visible:false},
                        plotOptions: {
                            pie: {
                                allowPointSelect: true,
                                cursor: 'pointer',
                                dataLabels: {
                                    enabled: false
                                },
                                showInLegend: true
                            }
                        },
                        tooltip: {
                            formatter: function () {
                                return '<b>' + this.point.name + '</b>: ' + this.percentage.toFixed(2) + ' %';
                            }
                        },
                        series: [{
                            type: 'pie',
                            name: 'zup?',
                            
                            data: [{name:'Maduro', y: item.totalChavismo, color:'#f00'}, ['Capriles', item.totalCapriles]]
                        }]
                    });
                    console.log(a);
                    return item;
                });
                */
            }
        });
    }

    function updateParticipacionLineal() {
        var options = {
            chart: {
                zoomType: 'x',
                renderTo: 'mws-line-chart-general'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },
            title: {
                text: 'Participacion'
            },
            xAxis: {
                type: 'datetime',
                tickWidth: 0,
                gridLineWidth: 1,
                labels: {
                    align: 'left',
                    x: 3,
                    y: -3
                }
            },
            yAxis: [{
                // left y axis
                title: {
                    text: null
                },
                labels: {
                    align: 'left',
                    x: 3,
                    y: 16,
                    formatter: function () {
                        return Highcharts.numberFormat(this.value, 0);
                    }
                },
                showFirstLabel: false
            }, {
                // right y axis
                linkedTo: 0,
                gridLineWidth: 0,
                opposite: true,
                title: {
                    text: null
                },
                labels: {
                    align: 'right',
                    x: -3,
                    y: 16,
                    formatter: function () {
                        return Highcharts.numberFormat(this.value, 0);
                    }
                },
                showFirstLabel: false
            }],
            tooltip: {
                shared: true,
                crosshairs: true
            },
            plotOptions: {
                series: {
                    cursor: 'pointer',
                    point: {
                        events: {
                            click: function () {
                                hs.htmlExpand(null, {
                                    pageOrigin: {
                                        x: this.pageX,
                                        y: this.pageY
                                    },
                                    headingText: this.series.name,
                                    maincontentText: Highcharts.dateFormat('%A, %b %e, %Y', this.x) + ':<br/> ' +
                                    this.y + ' visits',
                                    width: 200
                                });
                            }
                        }
                    },
                    marker: {
                        lineWidth: 1
                    }
                }
            },
            series: [
                {
                    type: 'column',
                    name: 'Cola'
                }, {
                    type: 'column',
                    name: 'Participacion Parcial'
                },
                {
                    name: 'Participacion'
                }]
        };
        $.getJSON('/Participacion/GetChartData', null, function (data) {
            var participacionArray = new Array();
            var participacionParcialArray = new Array();
            var colaArray = new Array();

            for (var i = 0; i < data.participacion.length; i++) {
                var utc = Date.UTC(data.participacion[i].year, data.participacion[i].month, data.participacion[i].day, data.participacion[i].hour, data.participacion[i].min);
                colaArray.push([utc, data.participacion[i].cola]);
                participacionArray.push([utc, data.participacion[i].participacion]);
                participacionParcialArray.push([utc, data.participacion[i].value]);
            }
            options.series[0].data = colaArray;
            options.series[1].data = participacionParcialArray;
            options.series[2].data = participacionArray;

            chartCentro = new Highcharts.Chart(options);
        });
    }

    $(document).ready(function () {
        $("#tabs").tabs({ cookie: { expires: 30} });
        $("#lnkTab1,#lnkTab2,#lnkTab3,#lnkTab4,#lnkTab5").click(function () { $(window).resize(); });
        $(document).bind('refreshStatsEvent', function () {
            //updateParticipacionMovilizacion();
            //updateParticipacionLineal();
            updateTotalizacion();
            updateIndicadores();
            updateAlertas();
            updateMatrizCalique();
            updateQuickCount();
            updateSustitucion();
            updateAlertasPorTendencia();
            updateTotalizacionPorTendencia();
        });

        $("#tag1,#tag2,#estado,#municipio,#parroquia").multiselect({
            noneSelectedText: 'Seleccione las filtros',
            selectedList: 4
        }).multiselectfilter();
        $("#tag1,#tag2").change(function () {
            $(document).trigger('refreshStatsEvent');
        });



        participacionChart = new Highcharts.Chart({
            chart: {
                zoomType: 'x',
                renderTo: 'participacionChart'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },
            title: {
                text: 'Participacion Porcentual'
            },
            xAxis: {
                type: 'datetime',
                tickWidth: 0,
                gridLineWidth: 1,
                labels: {
                    align: 'left',
                    x: 3,
                    y: -3
                }
            },

            yAxis: [{
                // left y axis
                title: {
                    text: null
                },
                labels: {
                    align: 'left',
                    x: 3,
                    y: 16,
                    formatter: function () {
                        return Highcharts.numberFormat(this.value, 0);
                    }
                },
                showFirstLabel: false
            }, {
                // right y axis
                linkedTo: 0,
                gridLineWidth: 0,
                opposite: true,
                title: {
                    text: null
                },
                labels: {
                    align: 'right',
                    x: -3,
                    y: 16,
                    formatter: function () {
                        return Highcharts.numberFormat(this.value, 0);
                    }
                },
                showFirstLabel: false
            }],
            tooltip: {
                shared: true,
                crosshairs: true
            },
            plotOptions: {
                series: {
                    cursor: 'pointer',
                    marker: {
                        lineWidth: 1
                    }
                }
            },
            series: [{ name: 'Participación', color: '#ff0000' }, { name: 'Movilizacion', color: '#00FF00' }, { name: 'Proyeccion Participacion', color: '#0000ff' }, { name: '% Movilizacion VS Participacion', color: '#aaaa00'}]
        }, function () {
            updateParticipacionMovilizacion();
        });
        alertasChart = new Highcharts.Chart({
            chart: {
                renderTo: 'alertasAcumuladasChart',
                type: 'bar'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },

            xAxis: {
                categories: [''],
                title: {
                    text: null
                },
                labels: { enabled: false }

            },
            title: {
                text: 'Alertas Acumuladas'
            },
            tooltip: {
                shared: false,
                crosshairs: true,
                formatter: function () {
                    return '<b>' + this.key + '</b>: ' + this.y;
                }
            },
            plotOptions: {
                series: {
                    cursor: 'pointer',
                    marker: {
                        lineWidth: 1
                    }
                }
            },
            series: [{ name: 'Alertas'}]
        });

        alertasPorEstadoChart = new Highcharts.Chart({
            chart: {
                renderTo: 'alertasPorEstadoChart',
                type: 'column'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },
            xAxis: {
                categories: [''],
                title: {
                    text: null
                },
                labels: { enabled: false }

            },
            title: {
                text: 'Alertas Por Estado'
            },
            tooltip: {
                shared: false,
                crosshairs: true,
                formatter: function () {
                    return '<b>' + this.key + '</b>: ' + this.y;
                }
            },
            plotOptions: {
                series: {
                    cursor: 'pointer',
                    marker: {
                        lineWidth: 1
                    }
                }
            },
            series: [{ name: 'Alertas'}]
        });

        alertasActivasChart = new Highcharts.Chart({
            chart: {
                renderTo: 'alertasActivasChart',
                type: 'bar'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },

            xAxis: {
                categories: [''],
                title: {
                    text: null
                },
                labels: { enabled: false }

            },
            title: {
                text: 'Alertas Activas'
            },
            tooltip: {
                shared: false,
                crosshairs: true,
                formatter: function () {
                    return '<b>' + this.key + '</b>: ' + this.y;
                }

            },
            plotOptions: {
                series: {
                    cursor: 'pointer',
                    marker: {
                        lineWidth: 1
                    }
                }
            },
            series: [{ name: 'Alertas'}]
        }, function () {
            updateAlertas();
        });



        totalizacionChart = new Highcharts.Chart({
            chart: {
                renderTo: 'totalizacionChart',
                plotBackgroundColor: null,
                plotBorderWidth: null,
                plotShadow: false
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },
            title: {
                text: 'Totalización'
            },
            tooltip: {
                formatter: function () {
                    return '<b>' + this.point.name + '</b>: ' + this.percentage.toFixed(2) + ' %';
                }
            },
            plotOptions: {
                pie: {
                    allowPointSelect: true,
                    cursor: 'pointer',
                    dataLabels: {
                        enabled: true,
                        color: '#000000',
                        connectorColor: '#000000',
                        formatter: function () {
                            return '<b>' + this.point.name + '</b>';
                        }
                    }, showInLegend: false
                }
            },
            series: [{
                type: 'pie',
                name: '',
                data: []
            }]
        },
            function () {
                updateTotalizacion();
            }
        );

        quickcountChart = new Highcharts.Chart({
            chart: {
                renderTo: 'quickcountChart',
                plotBackgroundColor: null,
                plotBorderWidth: null,
                plotShadow: false,
                type: 'bar'
            },
            credits: {
                href: null,
                text: "Generado por EdayRoom"
            },
            title: {
                text: 'Quick Count'
            },
            tooltip: {
                formatter: function () {
                    return '<b>' + this.point.name + '</b>: ' + this.percentage.toFixed(2) + ' %';
                }
            },
            plotOptions: {
                bar: {
                    stacking: 'percent',
                    dataLabels: {
                        enabled: true,
                        color: (Highcharts.theme && Highcharts.theme.dataLabelsColor) || 'white'
                    }
                }
            }

        },
            function () {
                updateQuickCount();
            }
        );

        $("#estado").change(function () {

            var states = "";
            if ($(this).val() != null)
                states = $(this).val().join(",");
            $.getJSON('/Dashboard/GetMunicipios', { states: states }, function (data) {
                $("#municipio").html('');
                $.each(data, function (key, value) {
                    $("#municipio").append($.format("<option>{0}</option>", value));
                });
                $("#municipio").multiselect('refresh');
                $("#municipio").change();
            });
            $(document).trigger('refreshStatsEvent');
        });
        $("#municipio").change(function () {
            if ($(this).val() != null) {
                var municipios = $(this).val().join(",");
            }
            $(document).trigger('refreshStatsEvent');
        });
        updateIndicadores();
        updateMatrizCalique();
        //updateSustitucion();
        updateQuickCount();
        //updateAlertasPorTendencia();
        //updateTotalizacionPorTendencia();
        updateProyeccionHistoricos();
        updateParticipacionLineal();
        setInterval(function () {
            $(document).trigger('refreshStatsEvent');
        }, 180000);
    });
    
