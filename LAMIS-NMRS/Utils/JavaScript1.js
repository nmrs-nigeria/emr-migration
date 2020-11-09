var person = {
    "names": [
        {
            "givenName": "string",
            "middleName": "string",
            "familyName": "string",
            "familyName2": "string",
            "preferred": true,
            "prefix": "string",
            "familyNamePrefix": "string",
            "familyNameSuffix": "string",
            "degree": "string"
        }
    ],
    "gender": "M",
    "age": 0,
    "birthdate": "string",
    "birthdateEstimated": true,
    "dead": true,
    "deathDate": "string",
    "causeOfDeath": "string",
    "addresses": [
        {
            "preferred": true,
            "address1": "string",
            "address2": "string",
            "cityVillage": "string",
            "stateProvince": "string",
            "country": "string",
            "postalCode": "string",
            "countyDistrict": "string",
            "address3": "string",
            "address4": "string",
            "address5": "string",
            "address6": "string",
            "startDate": "string",
            "endDate": "string",
            "latitude": "string",
            "longitude": "string"
        }
    ],
    "attributes": [
        {
            "attributeType": "uuid",
            "value": "string",
            "hydratedObject": "uuid"
        }
    ],
    "deathdateEstimated": true,
    "birthtime": "2020-11-06T16:07:04.989Z"
};

var encounter = {
    "patient": {
        "person": "uuid",
        "identifiers": [
            {
                "identifier": "string",
                "identifierType": "uuid",
                "location": "uuid",
                "preferred": true
            }
        ]
    },
    "encounterType": {
        "name": "string",
        "description": "string"
    },
    "encounterDatetime": "string",
    "location": {
        "name": "string",
        "description": "string",
        "address1": "string",
        "address2": "string",
        "cityVillage": "string",
        "stateProvince": "string",
        "country": "string",
        "postalCode": "string",
        "latitude": "string",
        "longitude": "string",
        "countyDistrict": "string",
        "address3": "string",
        "address4": "string",
        "address5": "string",
        "address6": "string",
        "tags": [
            "string"
        ],
        "parentLocation": "string",
        "childLocations": [
            "string"
        ]
    },
    "form": {
        "name": "string",
        "description": "string",
        "version": "string",
        "encounterType": "string",
        "build": 0,
        "published": true,
        "formFields": [
            "string"
        ],
        "xslt": "string",
        "template": "string"
    },
    "provider": "string",
    "orders": [
        {
            "encounter": "uuid",
            "action": "NEW",
            "accessionNumber": "string",
            "dateActivated": "string",
            "scheduledDate": "string",
            "patient": "uuid",
            "concept": "uuid",
            "careSetting": "uuid",
            "dateStopped": "string",
            "autoExpireDate": "string",
            "orderer": "uuid",
            "previousOrder": "uuid",
            "urgency": "ROUTINE",
            "orderReason": "uuid",
            "orderReasonNonCoded": "string",
            "instructions": "string",
            "commentToFulfiller": "string"
        }
    ],
    "obs": [
        null
    ]
};