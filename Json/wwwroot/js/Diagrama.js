function init() {
    var myDiagram = new go.Diagram('myDiagramDiv', {
        'undoManager.isEnabled': true,
        layout: new go.TreeLayout({
            angle: 90,
            path: go.TreePath.Source,
            arrangement: go.TreeArrangement.Horizontal
        })
    });

    function convertVisibility(v) {
        switch (v) {
            case 'public': return '+';
            case 'private': return '-';
            case 'protected': return '#';
            case 'package': return '~';
            default: return v;
        }
    }

    // Plantilla para propiedades
    var propertyTemplate = new go.Panel('Horizontal')
        .add(
            new go.TextBlock({ isMultiline: false, editable: false, width: 12 })
                .bind('text', 'visibility', convertVisibility),
            new go.TextBlock({ isMultiline: false, editable: true })
                .bindTwoWay('text', 'name')
                .bind('isUnderline', 'scope', s => s[0] === 'c'),
            new go.TextBlock('')
                .bind('text', 'type', t => t ? ': ' : ''),
            new go.TextBlock({ isMultiline: false, editable: true })
                .bindTwoWay('text', 'type'),
            new go.TextBlock({ isMultiline: false, editable: false })
                .bind('text', 'default', s => s ? ' = ' + s : '')
        );

    // Plantilla para métodos
    var methodTemplate = new go.Panel('Horizontal')
        .add(
            new go.TextBlock({ isMultiline: false, editable: false, width: 12 })
                .bind('text', 'visibility', convertVisibility),
            new go.TextBlock({ isMultiline: false, editable: true })
                .bindTwoWay('text', 'name')
                .bind('isUnderline', 'scope', s => s[0] === 'c'),
            new go.TextBlock('()')
                .bind('text', 'parameters', parr => {
                    var s = '(';
                    for (var i = 0; i < parr.length; i++) {
                        var param = parr[i];
                        if (i > 0) s += ', ';
                        s += param.name + ': ' + param.type;
                    }
                    return s + ')';
                }),
            new go.TextBlock('')
                .bind('text', 'type', t => t ? ': ' : ''),
            new go.TextBlock({ isMultiline: false, editable: true })
                .bindTwoWay('text', 'type')
        );

    // Plantilla para nodos
    myDiagram.nodeTemplate =
        new go.Node('Auto', {
            locationSpot: go.Spot.Center,
            fromSpot: go.Spot.AllSides,
            toSpot: go.Spot.AllSides
        })
            .add(
                new go.Shape({ fill: 'lightyellow' }),
                new go.Panel('Table', { defaultRowSeparatorStroke: 'black' })
                    .add(
                        new go.TextBlock({
                            row: 0, columnSpan: 2, margin: 3, alignment: go.Spot.Center,
                            font: 'bold 12pt sans-serif',
                            isMultiline: false, editable: true
                        })
                            .bindTwoWay('text', 'name'),
                        new go.TextBlock('Properties', { row: 1, font: 'italic 10pt sans-serif' })
                            .bindObject('visible', 'visible', v => !v, undefined, 'PROPERTIES'),
                        new go.Panel('Vertical', {
                            name: 'PROPERTIES',
                            row: 1,
                            margin: 3,
                            stretch: go.Stretch.Horizontal,
                            defaultAlignment: go.Spot.Left,
                            background: 'lightyellow',
                            itemTemplate: propertyTemplate
                        })
                            .bind('itemArray', 'properties'),
                        go.GraphObject.build("PanelExpanderButton", {
                            row: 1,
                            column: 1,
                            alignment: go.Spot.TopRight,
                            visible: false
                        }, "PROPERTIES")
                            .bind('visible', 'properties', arr => arr.length > 0),
                        new go.TextBlock('Methods', { row: 2, font: 'italic 10pt sans-serif' })
                            .bindObject('visible', 'visible', v => !v, undefined, 'METHODS'),
                        new go.Panel('Vertical', {
                            name: 'METHODS',
                            row: 2,
                            margin: 3,
                            stretch: go.Stretch.Horizontal,
                            defaultAlignment: go.Spot.Left,
                            background: 'lightyellow',
                            itemTemplate: methodTemplate
                        })
                            .bind('itemArray', 'methods'),
                        go.GraphObject.build("PanelExpanderButton", {
                            row: 2,
                            column: 1,
                            alignment: go.Spot.TopRight,
                            visible: false
                        }, "METHODS")
                            .bind('visible', 'methods', arr => arr.length > 0)
                    )
            );

    // Plantilla para los enlaces
    function linkStyle() {
        return { isTreeLink: false, fromEndSegmentLength: 0, toEndSegmentLength: 0 };
    }

    // Plantilla para las relaciones
    myDiagram.linkTemplate = new go.Link({
        ...linkStyle(),
        isTreeLink: true
    })
        .add(
            new go.Shape(),
            new go.Shape({ toArrow: 'Triangle', fill: 'white' })
        );

    // Plantilla para las relaciones entre clases
    myDiagram.linkTemplateMap.add('Association',
        new go.Link(linkStyle())
            .add(
                new go.Shape()
            ));

    myDiagram.linkTemplateMap.add('Realization',
        new go.Link(linkStyle())
            .add(
                new go.Shape({ strokeDashArray: [3, 2] }),
                new go.Shape({ toArrow: 'Triangle', fill: 'white' })
            ));

    myDiagram.linkTemplateMap.add('Dependency',
        new go.Link(linkStyle())
            .add(
                new go.Shape({ strokeDashArray: [3, 2] }),
                new go.Shape({ toArrow: 'OpenTriangle' })
            ));

    myDiagram.linkTemplateMap.add('Composition',
        new go.Link(linkStyle())
            .add(
                new go.Shape(),
                new go.Shape({ fromArrow: 'StretchedDiamond', scale: 1.3 }),
                new go.Shape({ toArrow: 'OpenTriangle' })
            ));

    // Datos de las clases (nodos)
    var nodedata = [
        // Clases de Herencia
        {
            key: 1,
            name: 'Employee',
            properties: [
                { name: 'name', type: 'String', visibility: 'public' },
                { name: 'id', type: 'Integer', visibility: 'public' },
                { name: 'salary', type: 'Decimal', visibility: 'public' }
            ],
            methods: [
                { name: 'getSalary', visibility: 'public' },
                { name: 'setSalary', visibility: 'private' }
            ]
        },
        {
            key: 2,
            name: 'Manager',
            properties: [
                { name: 'department', type: 'Department', visibility: 'public' }
            ],
            methods: [
                { name: 'assignTask', visibility: 'public' }
            ]
        },

        // Clases con Asociación
        {
            key: 3,
            name: 'Department',
            properties: [
                { name: 'departmentName', type: 'String', visibility: 'public' },
                { name: 'location', type: 'String', visibility: 'public' }
            ],
            methods: [
                { name: 'addEmployee', visibility: 'public' }
            ]
        },
        {
            key: 4,
            name: 'Project',
            properties: [
                { name: 'projectName', type: 'String', visibility: 'public' },
                { name: 'budget', type: 'Decimal', visibility: 'public' }
            ],
            methods: [
                { name: 'startProject', visibility: 'public' },
                { name: 'endProject', visibility: 'private' }
            ]
        },

        // Clases con Composición
        {
            key: 5,
            name: 'Task',
            properties: [
                { name: 'taskName', type: 'String', visibility: 'public' },
                { name: 'deadline', type: 'Date', visibility: 'public' }
            ],
            methods: [
                { name: 'assignTask', visibility: 'public' },
                { name: 'completeTask', visibility: 'private' }
            ]
        }
    ];

    // Relaciones entre clases
    var linkdata = [
        { from: 2, to: 1, relationship: 'Realization' },
        { from: 3, to: 1, relationship: 'Association' },
        { from: 4, to: 3, relationship: 'Association' },
        { from: 5, to: 4, relationship: 'Composition' }
    ];

    myDiagram.model = new go.GraphLinksModel(nodedata, linkdata);
}
init();