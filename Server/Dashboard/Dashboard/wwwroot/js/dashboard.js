$(document).ready(function () {
    let mapName, mapLength, mapWidth, mapResolution, mapData;
    let currentX, currentY;
    let client;
    let agvSpeed, agvMode;
    let coords = [];
    let currentView;
    let cellId, cellIndex, cellValue;
    let isCellSelectorChangedByUser = true;
    let isKeyPressed;
    let currentMap, currentMapIndex;
    let surveillanceCameraStreamLink, guidanceCameraStreamLink;
    let jsonResponse1, jsonResponse2, jsonResponse3;
    let isMqttConnected = false;
    let currentUnixTimestamp, previousUnixTimestamp;
    let agvStatus, agvLocation, agvId, agvWorkingMap, agvHardwareStatus;
    let maps = [];
    let mapNames = [];

    let map = {
        name: null,
        width: null,
        length: null,
        resolution: null,
        layers: {
            baseLayer: [],
            beaconLayer: [],
            packageLayer: [],
            lineLayer: []
        }
    };

    let cell = {
        x: null,
        y: null,
        cellType: null,
        cellValue: null
    };

    let cellType = new Map([
        ["Blank", "0"],
        ["Obstacle", "1"],
        ["Beacon", "*"],
        ["Package", "$"],
        ["Line", "-"],
        ["Rfid", "!"],
        ["AGV", "#"],
    ]);

    console.log('Loaded');
    agvId = $('#agvSelector').val();
    console.log('AGV ID = ' + agvId);
    //Event handlers for selectors
    $('#agvSelector').on('change', function () {
        agvId = $(this).find(':selected').val();
        console.log('AGV ID = ' + agvId);
    });
    $('#mapSelector').on('change', function () {
        currentMap = $(this).val();
        clearMapView();
        get_map(currentMap, 'baseLayer')
        init_map(mapLength, mapWidth, mapResolution);
        init_map_layer();
        render_map(mapData);
        console.log('Current map = ' + currentMap);
    });
    //Checking AGV mode selector
    agvMode = $('#modeSelector').val();
    console.log('AGV mode = ' + agvMode);
    $('#modeSelector').on('change', function () {
        agvMode = $(this).find(':selected').val();
        console.log('AGV mode = ' + agvMode);
    });
    //Checking camera view
    $('#cameraStreamView').addClass('hide');
    if ($('#cameraSwitch').is(':checked')) {
        console.log('Surveillance Camera ON');
        $('#cameraIndicator').html('ON');
    } else {
        console.log('Surveillance Camera OFF');
        $('#cameraIndicator').html('OFF');
    }
    //Checking camera switch
    $('#cameraSwitch').on('change', function () {
        if ($('#cameraSwitch').is(':checked')) {
            console.log('Surveillance Camera ON');
            $('#cameraIndicator').html('ON');
            $('#cameraStreamView').attr('src', surveillanceCameraStreamLink);
            $('#cameraStreamView').removeClass('hide');
        } else {
            console.log('Surveillance Camera OFF');
            $('#cameraIndicator').html('OFF');
            $('#cameraStreamView').attr('src', '');
            $('#cameraStreamView').addClass('hide');
        }
    });
    //Checking speed indicator
    $('#speedIndicator').html('Motor speed: ' + $('#speedSlider').val());
    agvSpeed = $('#speedSlider').val();
    //Watching on speed change
    document.getElementById('speedSlider').oninput = function () {
        agvSpeed = $('#speedSlider').val();
        console.log("AGV speed = " + agvSpeed);
        $('#speedIndicator').html('Motor speed: ' + $('#speedSlider').val());
        mqtt_publish('agv/control/' + agvId, 'speed ' + agvSpeed, 'move');
    };
    //Checking steering assist
    if ($('#steeringAssistSwitch').is(':checked')) {
        console.log('Steering Assist ON');
        $('#steeringAssistIndicator').html('Steering Assist: ON');
    } else {
        console.log('Steering Assist OFF');
        $('#steeringAssistIndicator').html('Steering Assist: OFF');
    }
    $('#steeringAssistSwitch').on('change', function () {
        if ($('#steeringAssistSwitch').is(':checked')) {
            console.log('Steering Assist ON');
            $('#steeringAssistIndicator').html('Steering Assist: ON');
        } else {
            console.log('Steering Assist OFF');
            $('#steeringAssistIndicator').html('Steering Assist: OFF');
        }
    });
    
    function update_map_data(name) {
        if (($('#editMapLength').val() !== mapLength) && ($('#editMapLength').val() !== '')) {
            mapLength = $('#editMapLength').val();
        }
        if (($('#editMapWidth').val() !== mapWidth) && ($('#editMapWidth').val() !== '')) {
            mapWidth = $('#editMapWidth').val();
        }
        mapData.coordinates = coords;
        mapData.length = mapLength;
        mapData.width = mapWidth;

        switch (mapName) {
            case "Warehouse 1":
                jsonResponse1 = JSON.stringify(mapData);
                console.log('New json string: ' + jsonResponse1)
                break;
            case "Warehouse 2":
                jsonResponse2 = JSON.stringify(mapData);
                console.log('New json string: ' + jsonResponse2)
                break;
            case "Warehouse 3":
                jsonResponse3 = JSON.stringify(mapData);
                console.log('New json string: ' + jsonResponse3)
                break;
        }

        console.log('Updated map data: ' + JSON.stringify(mapData))
    }

    function init_map(length, width, resolution) {
        previousCellType = '';
        let innerHtml = '';
        $('#editMapLength').attr('placeholder', length);
        $('#editMapWidth').attr('placeholder', width);

        //Create id with format: X(Width Index) = ' + j + ': Y(Length Index) = ' + i 
        for (let i = 0; i < length; i++) {
            innerHtml += '<tr class="d-flex m-0 p-0">';
            for (let j = 0; j < width; j++) {
                innerHtml += '<td class="d-flex m-0 p-0"><div class="squareCell border-end border-bottom border-secondary m-0 p-0 d-flex justify-content-center align-items-center" id="' + j + ':' + i + '"></div></td>';
            }
            innerHtml += '</tr>';
        }
        $('#mapViewView').removeClass('hide');
        $('#mapViewView').append(innerHtml);
        console.log('Init map done');

        $(".squareCell").click(function () {
            $('#cellCoordIndicator').text("Cell coordinate: " + this.id);
            let cellCoord = this.id.split(":");

            cellId = this.id;
            currentX = cellCoord[0];
            currentY = cellCoord[1];
            console.log(coords);
            cellIndex = (((parseInt(currentY) + 1) * mapWidth) - (mapWidth - currentX));
            cellType = coords[cellIndex].type;

            console.log('Cell type = ' + cellType);

            $('#cellTypeSelector option').removeAttr("selected");
            $('#cellTypeSelector option[value="' + cellType + '"]').attr('selected', 'selected');
            $("#cellTypeSelector").val(cellType).change();

            console.log('Cell index: ' + cellIndex);
            console.log('Cell with chosen coordinate : ' + this.id);
            console.log('Cell data');
            console.log(coords[cellIndex]);
        })

        $('#cellTypeSelector').on('change', function () {
            console.log('Change event');
            cellType = $(this).find(':selected').val();
            coords[cellIndex].type = cellType;
            render_cell(coords[cellIndex].value, cellType, currentX, currentY);

            console.log('Cell type has changed')
            console.log('New coords data: ' + coords[cellIndex].type)
        });
    }

    function clearMapView() {
        $('#mapViewView').empty();
        console.log('Cleared map view');
    }

    function clearEditMapPanel() {
        $('#cellCoordIndicator').text("Cell coordinate:");
        $('#mapLayerSelector').val("baseLayer");
        $('#cellTypeSelector').val("");
        $('#editCellValue').empty();
        $('#editMapLength').empty();
        $('#editMapWidth').empty();
    }

    function get_all_maps() {
        currentMapIndex = 0;

        mapNames = ['Warehouse 1', 'Warehouse 2', 'Warehouse 3'];
        jsonResponse1 = '{"name":"Warehouse 1", "length":4, "width":4, "resolution":20, "layer":"baseLayer", "coordinates":' +
            '[{"x":0, "y":0, "type":"*", "value": ""},{"x":1, "y":0, "type":"#", "value":"001"},' +
            '{"x":2, "y":0, "type":1, "value": ""},{"x":3, "y":0, "type":"1", "value": ""},' +
            '{"x":0, "y":1, "type":"0", "value": ""},{"x":1, "y":1, "type":"*", "value": ""},' +
            '{"x":2, "y":1, "type":"0", "value": ""},{"x":3, "y":1, "type":"0", "value": ""},' +
            '{"x":0, "y":2, "type":"1", "value": ""},{"x":1, "y":2, "type":"1", "value": ""},' +
            '{"x":2, "y":2, "type":"0", "value": ""},{"x":3, "y":2, "type":"0", "value": ""},' +
            '{"x":0, "y":3, "type":"0", "value": ""},{"x":1, "y":3, "type":"*", "value": ""},' +
            '{"x":2, "y":3, "type":"#", "value": "002"},{"x":3, "y":3, "type":"#", "value": "003"}]}';

        jsonResponse2 = '{"name":"Warehouse 2", "length":6, "width":6, "resolution":20,  "layer":"baseLayer", "coordinates":' +
            '[{"x":"0", "y":"0", "type":"0"},{"x":"1", "y":"0", "type":"#"},{"x":"2", "y":"0", "type":"1"},' +
            '{"x":"3", "y":"0", "type":"1"},{"x":"0", "y":"1", "type":"0"},{"x":"1", "y":"1", "type":"*"},' +
            '{"x":"2", "y":"1", "type":"0"},{"x":"3", "y":"1", "type":"0"},{"x":"0", "y":"2", "type":"1"},' +
            '{"x":"1", "y":"2", "type":"1"},{"x":"2", "y":"2", "type":"0"},{"x":"3", "y":"2", "type":"0"},' +
            '{"x":"0", "y":"3", "type":"0"},{"x":"1", "y":"3", "type":"*"},{"x":"2", "y":"3", "type":"#"},' +
            '{"x":"3", "y":"3", "type":"#"}]}';

        jsonResponse3 = '{"name":"Warehouse 3", "length":12, "width":10, "resolution":20,  "layer":"baseLayer", "coordinates":' +
            '[{"x":"0", "y":"0", "type":"0"},{"x":"1", "y":"0", "type":"#"},{"x":"2", "y":"0", "type":"1"},' +
            '{"x":"3", "y":"0", "type":"1"},{"x":"0", "y":"1", "type":"0"},{"x":"1", "y":"1", "type":"*"},' +
            '{"x":"2", "y":"1", "type":"0"},{"x":"3", "y":"1", "type":"0"},{"x":"0", "y":"2", "type":"1"},' +
            '{"x":"1", "y":"2", "type":"1"},{"x":"2", "y":"2", "type":"0"},{"x":"3", "y":"2", "type":"0"},' +
            '{"x":"0", "y":"3", "type":"0"},{"x":"1", "y":"3", "type":"*"},{"x":"2", "y":"3", "type":"#"},' +
            '{"x":"3", "y":"3", "type":"#"}]}';
        // Push all map data to maps array
        maps.push(JSON.parse(jsonResponse1));
        maps.push(JSON.parse(jsonResponse2));
        maps.push(JSON.parse(jsonResponse3));
    }

    function get_map(name, layerName) {
        // Find map by name and layer name in maps, using find function
        mapData = maps.find(function (_map) {
            return _map.name === name && _map.layer === layerName;
        });
        console.log('Get map data done, map data: ' + JSON.stringify(mapData));
        mapLength = mapData.length;
        mapWidth = mapData.width;
        mapResolution = mapData.resolution;
    }

    function init_map_layer() {
        baseLayer = mapData;
        beaconLayer = mapData;
        packageLayer = mapData;
    }

    function render_map(data) {
        let _mapData = data;

        mapName = _mapData.name;
        mapWidth = _mapData.width;
        mapLength = _mapData.length;
        mapResolution = _mapData.resolution;
        coords = _mapData.coordinates;

        // Add above elements to map
        map.name = mapName;
        map.width = mapWidth;
        map.length = mapLength;
        map.resolution = mapResolution;
        map.layers.baseLayer = coords;

        console.log('Base layer: ' + coords)

        let coordIndex = 0;

        for (let y = 0; y < mapLength; y++) {
            for (let x = 0; x < mapWidth; x++) {
                //console.log('coordIndex ' + coordIndex);
                //console.log("coords[coordIndex] = " + coords[coordIndex]);
                // Check if coords[coordIndex] is existing
                if (coords[coordIndex] !== undefined) {
                    let coord = coords[coordIndex];
                    console.log('Coord value = ' + coord.value);
                    console.log('Coord x = ' + coord.x);
                    console.log('Coord y = ' + coord.y);
                    render_cell(coord.value, coord.type, x, y);
                }
                coordIndex++;
            }
        }

        console.log('Render map done');
        //console.log(_mapData);
        //console.log(coords);
    }

    function render_cell(cellValue, cellType, x, y) {
        //# is represented for AGV
        if (cellType === '#') {
            console.log('AGV id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-success");
            cell.classList.remove("bg-transparent");
            cell.classList.remove("bg-danger");
            cell.classList.remove("bg-warning");
            cell.classList.remove("bg-info");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
            $("#" + x + '\\:' + y).append('<div class="blink d-flex m-0 p-2 text-success border border-white border-5 rounded-circle" id=' + cellValue + '>AGV</div>');
        } else if (cellType === '*') {
            //* is represented for Beacon
            //console.log('id = ' + x + ':' + y);
            console.log('Render beacon id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-transparent");
            cell.classList.remove("bg-info");
            cell.classList.remove("bg-danger");
            cell.classList.remove("bg-warning");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
            $("#" + x + '\\:' + y).append('<img class="blink d-flex m-0 p-2" src="https://img.icons8.com/?size=512&id=13064&format=png" width="50" height="50" alt=""/>');
        } else if (cellType === '1') {
            //1 is represented for Obstacle
            //console.log('id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-transparent");
            cell.classList.remove("bg-info");
            cell.classList.remove("bg-success");
            cell.classList.remove("bg-warning");
            cell.classList.add("bg-danger");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
        } else if (cellType === '0') {
            //0 is represented for Blank space
            //console.log('id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-danger");
            cell.classList.remove("bg-info");
            cell.classList.remove("bg-success");
            cell.classList.remove("bg-warning");
            cell.classList.add("bg-transparent");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
        } else if (cellType === '$') {
            //$ is represented for Package
            //console.log('id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-danger");
            cell.classList.remove("bg-info");
            cell.classList.remove("bg-success");
            cell.classList.remove("bg-transparent");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
            $("#" + x + '\\:' + y).append('<img class="d-flex m-0 p-2" src="https://img.icons8.com/?size=512&id=X3MGpXJOGVKe&format=png" width="50" height="50" alt=""/>');
        } else if (cellType !== '!') {
        } else {
            //! is represented for RFID Card
            //console.log('id = ' + x + ':' + y);
            let cell = document.getElementById(x + ':' + y);
            cell.classList.remove("bg-danger");
            cell.classList.remove("bg-success");
            cell.classList.remove("bg-warning");
            cell.classList.remove("bg-transparent");
            // Clear all child elements from JQuery
            $("#" + x + '\\:' + y).empty();
            $("#" + x + '\\:' + y).append('<img class="d-flex m-0 p-2" src="https://img.icons8.com/?size=512&id=45359&format=png" width="50" height="50" alt=""/>');
        }

        console.log('Rendered cell');
    }

    function init_mqtt() {
        let mqttBrokerUrl = 'ws://pirover.xyz:9001';
        client = mqtt.connect(mqttBrokerUrl);

        client.on("connect", function () {
            client.subscribe("agv/control/" + agvId);
            client.subscribe("agv/status");
            client.subscribe("agv/package/delivery");

            console.log('Init MQTT done');
            isMqttConnected = true;
        });

        client.on('message', function (topic, message) {
            //console.log("Message = " + message.toString())
            switch (topic) {
                case "agv/status":
                    let _message = message.toString();
                    console.log(_message);
                    agvStatus = JSON.parse(_message);
                    if (agvStatus.id === agvId) {
                        $("#agvConnectingIndicator").removeClass("text-danger");
                        $("#agvConnectingIndicator").addClass("text-primary");
                        $("#agvConnectingIndicator").text("Connected");
                        $("#agvWorkingMapIndicator").removeClass("text-danger");
                        $("#agvWorkingMapIndicator").addClass("text-primary");
                        $("#agvWorkingMapIndicator").text("Current working map: " + agvStatus.workingMap);
                        $("#agvCoordinatesIndicator").removeClass("text-danger");
                        $("#agvCoordinatesIndicator").addClass("text-primary");
                        $("#agvCoordinatesIndicator").text("Current position: x = " + agvStatus.location.x + ";y = " + agvStatus.location.y);
                    }
                    animate_agv_status(agvStatus.id, agvStatus.location.x, agvStatus.location.y);
                    break;
                case "agv/control/" + agvId:
                    break;
                case "agv/package/delivery":
                    break;
                case "agv/package/location":
                    break;
            }

            //console.log([topic, message].join(": "));
        });
    }

    function mqtt_publish(topic, message, type) {
        let msg;
        if (type === 'move') {
            //msg = {
            //    controller: 'motor-controller',
            //    command: message,
            //}
            client.publish(topic, message);
        }
        console.log('Sent ' + topic + " " + message);
    }

    function init_nav_buttons() {
        $('#previousMap').click(function () {
            if (currentMapIndex > 0) {
                currentMapIndex--;
            } else if (currentMapIndex === 0) {
                currentMapIndex = mapNames.length - 1;
            }
            currentMap = mapNames[currentMapIndex];
            $('#mapSelector').val(mapNames[currentMapIndex]);
            $('#mapSelector').trigger("change")

            console.log('Current map index = ' + currentMapIndex)
            console.log('Render previous map')
        });

        $('#nextMap').click(function () {
            if (currentMapIndex < (mapNames.length - 1)) {
                currentMapIndex++;
            } else if (currentMapIndex === (mapNames.length - 1)) {
                currentMapIndex = 0;
            }
            currentMap = mapNames[currentMapIndex];
            $('#mapSelector').val(mapNames[currentMapIndex]);
            $('#mapSelector').trigger("change")

            console.log('Current map index = ' + currentMapIndex)
            console.log('Render next map')
        });

        $('#beaconScannerButton').click(function () {
            if (currentView !== 'beaconScannerView') {
                $('#beaconScannerView').removeClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'beaconScannerView';
            }
            console.log('beaconScannerButton clicked');
        });

        $('#laserScannerButton').click(function () {
            if (currentView !== 'laserScannerView') {
                $('#laserScannerView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'deliveryControlView';
            }
            console.log('deliveryControlButton clicked');
        });

        $('#mapViewButton').click(function () {
            if (currentView !== 'mapViewView') {
                $('#mapViewView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'mapViewView';
            }
            console.log('mapViewButton clicked');
        });

        $('#deliveryControlButton').click(function () {
            if (currentView !== 'deliveryControlView') {
                $('#deliveryControlView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'deliveryControlView';
            }
            console.log('deliveryControlButton clicked');
        });

        $('#rfidReaderButton').click(function () {
            if (currentView !== 'rfidReaderView') {
                $('#rfidReaderView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'rfidReaderView';
            }
            console.log('rfidReaderButton clicked');
        });

        $('#mapLinkButton').click(function () {
            if (currentView !== 'mapLinkView') {
                $('#mapLinkView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'mapLinkView';
            }
            console.log('mapLinkButton clicked');
        });

        $('#computerVisionButton').click(function () {
            if (currentView !== 'computerVisionView') {
                $('#computerVisionView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'computerVisionView';
            }
            console.log('computerVisionButton clicked');
        });

        $('#beaconMeshButton').click(function () {
            if (currentView !== 'beaconMeshView') {
                $('#beaconMeshView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                $('#qrScannerView').addClass('d-none');
                currentView = 'beaconMeshView';
            }
            console.log('beaconMeshButton clicked');
        });

        $('#qrScannerButton').click(function () {
            if (currentView !== 'qrScannerView') {
                $('#qrScannerView').removeClass('d-none');
                $('#beaconScannerView').addClass('d-none');
                $('#laserScannerView').addClass('d-none');
                $('#mapViewView').addClass('d-none');
                $('#rfidReaderView').addClass('d-none');
                $('#mapLinkView').addClass('d-none');
                $('#computerVisionView').addClass('d-none');
                $('#beaconMeshView').addClass('d-none');
                $('#deliveryControlView').addClass('d-none');
                currentView = 'qrScannerView';
            }
            console.log('qrScannerButton clicked');
        });


    }

    function init_control_buttons() {
        isKeyPressed = false;
        //For clicking button
        $('#goForwardButton').click(function () {
            if ((agvMode === 'direct') && (agvId !== 'Select AGV')) {
                mqtt_publish('agv/control/' + agvId, 'forward', 'move');
                console.log('Go Forward');
            } else {
                alert('AGV ID or Direct mode is not chosen');
            }
        });

        $('#goBackwardButton').click(function () {
            if ((agvMode === 'direct') && (agvId !== 'Select AGV')) {
                mqtt_publish('agv/control/' + agvId, 'backward', 'move');
                console.log('Go Backward');
            } else {
                alert('AGV ID or Direct mode is not chosen');
            }
        });

        $('#turnLeftButton').click(function () {
            if ((agvMode === 'direct') && (agvId !== 'Select AGV')) {
                mqtt_publish('agv/control/' + agvId, 'turn-left', 'move');
                console.log('Turn Left');
            } else {
                alert('AGV ID or Direct mode is not chosen');
            }
        });

        $('#turnRightButton').click(function () {
            if ((agvMode === 'direct') && (agvId !== 'Select AGV')) {
                mqtt_publish('agv/control/' + agvId, 'turn-right', 'move');
                console.log('Turn Right');
            } else {
                alert('AGV ID or Direct mode is not chosen');
            }
        });

        //For pressing physical key
        $(document).keydown(function (e) {
            if ((e.key === 'w') || (e.key === 'W')) {
                let button = document.getElementById('goForwardButton');
                button.classList.remove('bg-info');
                button.classList.add('bg-success');
                $('#goForwardButton').click();
            }
            if ((e.key === 's') || (e.key === 'S')) {
                let button = document.getElementById('goBackwardButton');
                button.classList.remove('bg-info');
                button.classList.add('bg-success');
                $('#goBackwardButton').click();
            }
            if ((e.key === 'a') || (e.key === 'A')) {
                let button = document.getElementById('turnLeftButton');
                button.classList.remove('bg-info');
                button.classList.add('bg-success');
                $('#turnLeftButton').click();
            }
            if ((e.key === 'd') || (e.key === 'D')) {
                let button = document.getElementById('turnRightButton');
                button.classList.remove('bg-info');
                button.classList.add('bg-success');
                $('#turnRightButton').click();
            }
            //console.log(e.key);
        });

        //Free key when stop pressing
        $(document).keyup(function (e) {
            if ((e.key === 'w') || (e.key === 'W')) {
                let button = document.getElementById('goForwardButton');
                button.classList.remove('bg-success');
                button.classList.add('bg-info');
                mqtt_publish('agv/control/' + agvId, 'stop', 'move');
                console.log('Released button');
            }
            if ((e.key === 's') || (e.key === 'S')) {
                let button = document.getElementById('goBackwardButton');
                button.classList.remove('bg-success');
                button.classList.add('bg-info');
                mqtt_publish('agv/control/' + agvId, 'stop', 'move');
                console.log('Released button');
            }
            if ((e.key === 'a') || (e.key === 'A')) {
                let button = document.getElementById('turnLeftButton');
                button.classList.remove('bg-success');
                button.classList.add('bg-info');
                mqtt_publish('agv/control/' + agvId, 'stop', 'move');
                console.log('Released button');
            }
            if ((e.key === 'd') || (e.key === 'D')) {
                let button = document.getElementById('turnRightButton');
                button.classList.remove('bg-success');
                button.classList.add('bg-info');
                mqtt_publish('agv/control/' + agvId, 'stop', 'move');
                console.log('Released button');
            }
            console.log(e.key);
        });
    }

    function init_right_tab_buttons() {
        $('#saveMapButton').click(function () {
            update_map_data();
            console.log('Saved map data');

            clearEditMapPanel();
            $('#mapSelector').trigger("change");

        });
    }

    function get_direction(id, x, y, _agvLocation_x, _agvLocation_y) {
        // Get current position of agv
        let current_x = _agvLocation_x;
        let current_y = _agvLocation_y;
        // Get direction of new position to the current position
        let direction = '';
        if (x > current_x) {
            direction = 'right';
        } else if (x < current_x) {
            direction = 'left';
        } else if (y > current_y) {
            direction = 'down';
        } else if (y < current_y) {
            direction = 'up';
        }
        console.log('Direction: ' + direction);
        return direction;
    }

    function get_cell(x, y) {
        // From new position, get cell element which has id = x:y
        return $('#' + x + '\\:' + y);
    }

    function get_location(cellType, cellValue) {
        let location = {
            x: null,
            y: null
        }
        // Match index of element in base layer
        _baseLayer = map.layers.baseLayer;
        console.log("Getting location: with cellType = " + cellType + " and cellValue = " + cellValue)
        // Loop through base layer array to find element which matches cellType and cellValue
        for (let i = 0; i < _baseLayer.length; i++) {
            console.log("Checking " + _baseLayer[i].type + " " + _baseLayer[i].value + " at " + _baseLayer[i].x + ":" + _baseLayer[i].y)
            if ((_baseLayer[i].type === cellType) && (_baseLayer[i].value === cellValue)) {
                console.log("Found match")
                location.x = _baseLayer[i].x;
                location.y = _baseLayer[i].y;
                break;
            }
        }
        return location;
    }

    function update_map(_mapName, _layerName, _cellType, _cellValue, _x, _y) {
        console.log("Updating map with mapName = " + _mapName + ", layerName = " + _layerName + ", cellType = " + _cellType + ", cellValue = " + _cellValue + ", x = " + _x + ", y = " + _y)
        // Update layer data
        let _layer = map.layers[_layerName];
        // Update cell data
        let _cell = {
            x: _x,
            y: _y,
            type: _cellType,
            value: _cellValue
        }
        // Check if cell is already in layer
        let _cellIndex = _layer.findIndex(cell => cell.x === _x && cell.y === _y);
        if (_cellIndex === -1) {
            // If cell is not in layer, push cell to layer
            console.log("Cell is not in layer")
        } else {
            // If cell is in layer, replace cell
            console.log("Cell is in layer. Start replacing")
            _layer[_cellIndex] = _cell;
            map.layers[_layerName] = _layer;
        }
        // Loop over maps array to find map which matches mapName
        for (let i = 0; i < maps.length; i++) {
            console.log("Current map " + JSON.stringify(maps[i]))
            if (maps[i].name === _mapName) {
                // If map is found, replace map
                maps[i] = map;
                console.log("Updated map " + JSON.stringify(maps[i]));
                break;
            }
        }
        console.log('Updated map ' + _mapName + ' layer ' + _layerName + ' with cell ' + _cellType + ' ' + _cellValue + ' at ' + _x + ':' + _y);
    }

    function animate_agv_status(id, x, y) {
        // Get agv current location from base layer
        let agvLocation = get_location('#', id);
        // Show agvLocation and x,y
        console.log("AGV current location: " + agvLocation.x + ':' + agvLocation.y);
        console.log("New location: " + x + ':' + y);
        // Only execute if there is not any AGV/Beacon/Package/Obstacle 
        // at the new position and x or y is different from current position
        targetCellType = map.layers.baseLayer.findIndex(cell => cell.x === x && cell.y === y);
        targetCellType = map.layers.baseLayer[targetCellType].type;
        console.log("Target cell type: " + targetCellType);
        if (((agvLocation.x !== x) || (agvLocation.y !== y)) && 
            ((targetCellType === '0') || (targetCellType === '!') || (targetCellType === '-'))) {
            console.log('animate_agv agv ' + id + ' to ' + x + ', ' + y);
            // From new position, get cell element which has id = x:y
            let targetCell = $('#' + x + '\\:' + y);
            // Empty new cell
            targetCell.empty();
            // Get agv element
            let agv = $('#' + id);
            // Get agv offset
            let old_agv_offset = agv.offset();
            // Show agvLocation
            console.log("AGV current location: " + agvLocation.x + ':' + agvLocation.y);
            // Clone agv to animate_agv
            let agv_clone = agv.clone();
            // Append clone to map view
            agv_clone.appendTo($('#mapViewView'));
            // Show clone
            agv_clone.show();
            // Append agv to new cell
            agv.appendTo(targetCell);
            // Get new offset of original agv
            let new_agv_offset = agv.offset();
            // Set clone position to be the same as original
            agv_clone.css({
                position: 'absolute',
                left: old_agv_offset.left,
                top: old_agv_offset.top,
                zIndex: 1000
            });
            // With old location, remove agv from old cell
            let oldCell = get_cell(agvLocation.x, agvLocation.y);
            oldCell.empty();
            // Hide original agv
            agv.hide();
            // animate_agv clone to new position to original agv
            agv_clone.animate({
                position: 'absolute',
                left: new_agv_offset.left,
                top: new_agv_offset.top
            }, 'slow', function () {
                // Remove clone after animation
                console.log("Animation done");
                agv_clone.remove();
                // Show original agv
                agv.show();
            });
            // Update agv current location to new cell
            update_map(mapName, 'baseLayer', '#', id, x, y);
            // Remove agv from old cell
            update_map(mapName, 'baseLayer', '0', null, agvLocation.x, agvLocation.y);
        } else {
            console.log("AGV is at the same position");
        }
    }

    function animate_beacon_cover_range() {

    }
    
    function animate_beacon_status() {

    }
    
    function check_agv_connection_status() {
        // Get current timestamp as Unix time
        currentUnixTimestamp = Math.floor(Date.now() / 1000);
        //console.log('currentUnixTimestamp: ' + currentUnixTimestamp);
        if (isMqttConnected && agvStatus != null && agvStatus.id === agvId) {
            //console.log('agvStatus.timestamp: ' + agvStatus.timestamp);
            // If difference is more than 5 seconds, consider it disconnected
            if ((currentUnixTimestamp - agvStatus.timestamp) > 5) {
                $("#agvConnectingIndicator").addClass("text-danger");
                $("#agvConnectingIndicator").removeClass("text-primary");
                $("#agvConnectingIndicator").text("Not connected");
                $("#agvWorkingMapIndicator").addClass("text-danger");
                $("#agvWorkingMapIndicator").removeClass("text-primary");
                $("#agvWorkingMapIndicator").text("Current working map:");
                $("#agvCoordinatesIndicator").addClass("text-danger");
                $("#agvCoordinatesIndicator").removeClass("text-primary");
                $("#agvCoordinatesIndicator").text("Current position:");
            }
        }
    }

    function main() {
        //Init Map view
        get_all_maps();
        //Init MQTT connection and necessary buttons
        init_mqtt();
        init_control_buttons();
        init_nav_buttons();
        init_right_tab_buttons();
    }

    main();

    setInterval(function () {
        // Check AGV connection status
        check_agv_connection_status();
    }, 1000);
});