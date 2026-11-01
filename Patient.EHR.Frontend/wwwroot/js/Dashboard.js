
let patients = [];
let clinical = {};
let activePatientId = null;
let currentTab = "demographics"; const API_BASE_URL = "/api/patient";
async function loadPatients() {
    try {
        console.log("Calling API:", `${API_BASE_URL}/all`);          
        const response = await fetch( `${API_BASE_URL}/all`);
        const data = await response.json();
        patients = data.map(p => ({
            id: (p.PatientId || '').toString(),
            firstName: p.FirstName || '',
            lastName: p.LastName || '',
            mrn: p.MRN || '',
            name: `${p.FirstName || ''} ${p.LastName || ''}`.trim(),
            ssn: p.SSN || '',
            gender: p.Gender || '',
            contact: (p.PhoneNumber || p.Email || '').toString(),
            blood: p.BloodGroup || '',
            lastVisit: p.CreatedDate
                ? p.CreatedDate.substring(0, 10)
                : ''
        }));

        if (patients.length > 0) {
            activePatientId = patients[0].id;
        }

        console.log("Patients Loaded:", patients);

        renderPatients();
    }
    catch (error) {
        console.error("Load Patients Error:", error);
    }
}
function ensureClinical(id) {
    if (!clinical[id]) {
        clinical[id] = {
            chief: "",
            hpi: "",
            pmh: "",
            plan: "",
            vitals: {
                bp: "",
                hr: "",
                temp: "",
                rr: "",
                spo2: "",
                weight: ""
            },
            docs: []
        };
    }
    return clinical[id];
}
function toast(msg) {
    const t = document.getElementById('toast');
    t.textContent = msg;
    t.classList.add('show');
    clearTimeout(window._toastTimer);
    window._toastTimer =
        setTimeout(() => t.classList.remove('show'), 2200);
}
function getPatient(id) {
    return patients.find(p => p.id === id);
}
function escapeHtml(value) {
    return String(value ?? '').replace(/[&<>"']/g, character => ({
        '&': '&amp;',
        '<': '&lt;',
        '>': '&gt;',
        '"': '&quot;',
        "'": '&#39;'
    })[character]);
}
function contextBar(containerId) {
    const p = getPatient(activePatientId);
    const el = document.getElementById(containerId);
    if (!el) return;
    if (!p) {
        el.innerHTML = '';
        return;
    }
    el.innerHTML = `
        <div class="patient-context">
            <i class="fa-solid fa-user"></i>
            Viewing record for <b>${escapeHtml(p.name)}</b> (${escapeHtml(p.id)})
        </div>
    `;
}
/* ============ RENDER: Patient table ============ */
function renderPatients(filter = "") {
    const tbody = document.getElementById('patientTableBody');
    if (!tbody) return;
    const f = (filter || "").toLowerCase();
    const rows = patients.filter(p =>!f ||
        (p.firstName || '').toLowerCase().includes(f) ||
        (p.lastName || '').toLowerCase().includes(f) ||
        (p.mrn || '').toLowerCase().includes(f)
    );
    console.log("Rendering Rows:", rows);
    if (rows.length === 0) {
        tbody.innerHTML = `
            <tr>
                <td colspan="8">
                    <div class="empty-state">
                        No patients match your search.
                    </div>
                </td>
            </tr>
        `;
        return;
    }
    tbody.innerHTML = rows.map(p => `
        <tr class= "patient-row" data-patient="${p.id}">
            <td>
                <span class="patient-id-link" data-select="${p.id}">
                    ${escapeHtml(p.id)}
                </span>
            </td>
            <td class="patient-name">${escapeHtml(p.name)} </td>
            <td>${escapeHtml(p.ssn)}</td>
            <td>${escapeHtml(p.gender)}</td>
            <td>${escapeHtml(p.contact)}</td>
            <td>
                <span class="badge">
                    ${escapeHtml(p.blood)}
                </span>
            </td>
            <td>${escapeHtml(p.lastVisit)}</td>
            <td>
                <div class="row-actions">
                    <i class="fa-solid fa-pen"
                       title="Edit"
                       data-edit="${p.id}">
                    </i>
                    <i class="fa-solid fa-ellipsis-vertical"
                       title="More"
                       data-more="${p.id}">
                    </i>
                </div>
            </td>
        </tr>
    `).join('');
}
/* ============ RENDER: Documents ============ */
async function renderDocs() {

    contextBar('docsContext');

    const grid =
        document.getElementById('docGrid');

    if (!activePatientId) {

        grid.innerHTML =
            `<div class="empty-state">
                Select a patient first.
             </div>`;

        return;
    }

    const response =
        await fetch(
            `${API_BASE_URL}/documents/${activePatientId}`
        );

    const docs =
        await response.json();

    if (!docs.length) {

        grid.innerHTML =
            `<div class="empty-state"
                   style="grid-column:1/-1;">
                No documents uploaded yet.
             </div>`;

        return;
    }

    grid.innerHTML =
        docs.map(d => `
            <div class="doc-card">

                <div class="doc-icon">
                    <i class="fa-solid fa-file"></i>
                </div>

                <div>
                    <div class="doc-name">
                        ${escapeHtml(d.DocumentName)}
                    </div>

                    <div class="doc-meta">
                        ${escapeHtml(d.DocumentType)}
                    </div>
                </div>

            </div>
        `).join('');
}
/* ============ TAB SWITCHING ============ */
function showTab(tab) {
    currentTab = tab;
    document.querySelectorAll('.tab-btn').forEach(button => {
        const isActive = button.dataset.tab === tab;
        button.classList.toggle('active', isActive);
        button.setAttribute('aria-selected', String(isActive));
    });
    document.querySelectorAll('.tab-panel').forEach(panel => {
        panel.classList.toggle('active', panel.id === `panel-${tab}`);
    });

    if (tab === 'demographics')
    {
        renderPatients( document.getElementById('tableFilter')?.value || '');
    }
    if (tab === 'encounter') {
        contextBar('encounterContext');
        if (activePatientId) {
            loadEncounter(activePatientId);
        } else {
            toast("Please select a patient first.");
        }
    }
    if (tab === 'docs') { renderDocs(); }
}

document.getElementById('tabsRow').addEventListener('click', e => {
    const btn = e.target.closest('.tab-btn');
    if (btn) showTab(btn.dataset.tab);
});

/* ============ SEARCH ============ */
document.getElementById('patientSearch').addEventListener('input', async function () {
        const term = this.value.trim();
        const results =
            document.getElementById(
                'searchResults'
            );
        if (term.length < 2) {
            results.style.display = 'none';
            return;
        }
        const response = await fetch(
            `${API_BASE_URL}/search?term=${encodeURIComponent(term)}`
        );
        const matches =
            await response.json();
        if (matches.length === 0) {
            results.innerHTML =
                `<div class="search-item">
                No Patient Found
             </div>`;
            results.style.display = 'block';
            return;
        }
    results.innerHTML = matches.map(p => `
<div class="search-item"
     data-id="${escapeHtml(p.PatientId)}">
    <div>
        <div class="search-name">
            ${escapeHtml(p.FirstName)} ${escapeHtml(p.LastName)}
        </div>
        <div class="search-details">
    <i class="fa-solid fa-calendar-days"></i>
    ${
        p.DOB
            ? escapeHtml(p.DOB.substring(0, 10))
            : 'N/A'
        }

    &nbsp;&nbsp;

    <i class="fa-solid fa-phone"></i>
    ${escapeHtml(p.PhoneNumber || 'N/A')}
</div>
    </div>
    <div class="search-arrow">
        <i class="fa-solid fa-chevron-right"></i>
    </div>
</div>
`).join('');
        results.style.display = 'block';
    });

document.getElementById('searchResults')
    .addEventListener('click', function (e) {

        const row = e.target.closest('.search-item');

        if (!row) return;

        const patientId = row.dataset.id;

        activePatientId = patientId;

        loadPatientForEdit(patientId);

        this.style.display = 'none';
    });

document.getElementById('tableFilter').addEventListener('input', function () { renderPatients(this.value);
    });

/* ============ SELECT / EDIT PATIENT ROW ============ */
document.getElementById('patientTableBody').addEventListener('click', async e => {const row = e.target.closest('.patient-row');
        if (
            row &&
            !e.target.closest('[data-edit]') &&
            !e.target.closest('[data-more]')
        ) {
            document .querySelectorAll('.patient-row') .forEach(r => r.classList.remove('active-row'));
            row.classList.add('active-row');
            loadPatientForEdit(
                row.dataset.patient
            );
            return;
        }
        const sel = e.target.closest('[data-select]');
        const edit = e.target.closest('[data-edit]');
        const more = e.target.closest('[data-more]');
        if (sel) {activePatientId =sel.dataset.select;
            loadEncounter( activePatientId );
            toast(
                `${getPatient(activePatientId).name} selected`
            );
        }
        if (edit) {
            loadPatientForEdit(
                edit.dataset.edit
            );
        }
        if (more) {
            if (
                confirm(
                    `Remove ${getPatient(more.dataset.more).name} from the patient list?`
                )
            ) {
                const response = await fetch(`${API_BASE_URL}/${more.dataset.more}`, {
                    method: "DELETE"
                });
                if (response.ok) {
                    patients = patients.filter(p => p.id !== more.dataset.more);
                    if (activePatientId === more.dataset.more) {
                        activePatientId = patients[0]?.id || null;
                    }
                    renderPatients(document.getElementById('tableFilter')?.value || '');
                    toast('Patient removed.');
                } else {
                    toast('Failed to remove patient.');
                }
            }
        }
    });

/* ============ MODAL MANAGEMENT ============ */
function openModal(id) { document.getElementById(id).classList.add('open'); }
function closeModal(id) { document.getElementById(id) .classList.remove('open');
    if (id === 'addPatientModal') {
        document .querySelectorAll('.patient-row').forEach(r => r.classList.remove('active-row'));
    }
}

document.getElementById('addPatientBtn')
    .addEventListener('click', () => {

        clearPatientForm();

        window.editPatientId = null;

        const kicker =
            document.getElementById(
                'patientModalKicker'
            );

        const title =
            document.getElementById(
                'patientModalTitle'
            );

        if (kicker) {
            kicker.textContent =
                'PATIENT REGISTRATION';
        }

        if (title) {
            title.textContent =
                'New Patient Profile';
        }

        openModal('addPatientModal');
    });
document.querySelectorAll('[data-close]').forEach(el => {
    el.addEventListener('click', () => closeModal(el.dataset.close));
});

/* ============ ADD PATIENT ============ */
function clearPatientForm() {

    window.editPatientId = null;

    const form =
        document.getElementById('addPatientForm');

    form.reset();

    document.getElementById('MRN').value = '';
    document.getElementById('firstname').value = '';
    document.getElementById('lastname').value = '';
    document.getElementById('dob').value = '';
    document.getElementById('SSN').value = '';
    document.getElementById('phonenumber').value = '';
    document.getElementById('alternatenumber').value = '';
    document.getElementById('email').value = '';
    document.getElementById('emergencycontact').value = '';
    document.getElementById('emergencyrelationship').value = '';
    document.getElementById('streetaddress').value = '';
    document.getElementById('City').value = '';
    document.getElementById('State').value = '';
    document.getElementById('Zipcode').value = '';
    document.getElementById('ward').value = '';
    document.getElementById('room').value = '';
    document.getElementById('referringphysician').value = '';
    document.getElementById('insuranceprovider').value = '';
    document.getElementById('policynumber').value = '';
    document.getElementById('dependentname1').value = '';
    document.getElementById('dependentrelationship1').value = '';
    document.getElementById('dependentdob1').value = '';
    document.getElementById('dependentname2').value = '';
    document.getElementById('dependentrelationship2').value = '';
    document.getElementById('dependentdob2').value = '';
    document.getElementById('gender').selectedIndex = 0;
    document.getElementById('bloodgroup').selectedIndex = 0;
    document.getElementById('maritalstatus').selectedIndex = 0;
    document.getElementById('admissiontype').selectedIndex = 0;
    document.getElementById('insurancetype').selectedIndex = 0;

    document.querySelectorAll('.patient-tab')
        .forEach(t => t.classList.remove('active'));

    document.querySelectorAll('.patient-panel')
        .forEach(p => p.classList.remove('active'));

    document.querySelector('[data-tab="personal"]')
        .classList.add('active');

    document.getElementById('personal-panel')
        .classList.add('active');
}



document.getElementById('addPatientForm').addEventListener('submit', async function (e) {
    e.preventDefault();

    const patient = {
        PatientId: window.editPatientId ? Number(window.editPatientId) : 0,
        MRN: document.getElementById('MRN').value,
        FirstName: document.getElementById('firstname').value,
        LastName: document.getElementById('lastname').value,
        DOB: document.getElementById('dob').value,
        Gender: document.getElementById('gender').value,
        SSN: document.getElementById('SSN').value,
        BloodGroup: document.getElementById('bloodgroup').value,
        MaritalStatus: document.getElementById('maritalstatus').value,
        Nationality: document.getElementById('nationality').value,
        PhoneNumber: document.getElementById('phonenumber').value,
        AlternateNumber: document.getElementById('alternatenumber').value,
        Email: document.getElementById('email').value,
        EmergencyContact: document.getElementById('emergencycontact').value,
        EmergencyRelationship: document.getElementById('emergencyrelationship').value,
        StreetAddress: document.getElementById('streetaddress').value,
        City: document.getElementById('City').value,
        State: document.getElementById('State').value,
        ZipCode: document.getElementById('Zipcode').value,
        AdmissionType: document.getElementById('admissiontype').value,
        Ward: document.getElementById('ward').value,
        Room: document.getElementById('room').value,
        ReferringPhysician: document.getElementById('referringphysician').value,
        InsuranceProvider: document.getElementById('insuranceprovider').value,
        PolicyNumber: document.getElementById('policynumber').value,
        InsuranceType: document.getElementById('insurancetype').value, 
    };

    if (window.editPatientId) {
        patient.PatientId = Number(window.editPatientId);
    }
    const request = {
        Patient: patient,
        Dependents: [
            {
                DependentName:
                    document.getElementById('dependentname1').value,
                Relationship:
                    document.getElementById('dependentrelationship1').value,
                DOB:
                    document.getElementById('dependentdob1').value
            },
            {
                DependentName:
                    document.getElementById('dependentname2').value,
                Relationship:
                    document.getElementById('dependentrelationship2').value,
                DOB:
                    document.getElementById('dependentdob2').value
            }
        ]
    };
    console.log("Request Payload:", request);

    try {
        if (window.editPatientId) {
            patient.PatientId = Number(window.editPatientId);
        }
        const url = window.editPatientId
            ? `${API_BASE_URL}/update`
            : `${API_BASE_URL}/add`;
        const method = window.editPatientId
            ? "PUT"
            : "POST";
        const response = await fetch(url, {
            method: method,
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(request)
        });
        console.log("Response Status:", response.status);
        if (response.ok) {
            if (window.editPatientId) {
               toast("Patient updated successfully");
            } else {
              toast("Patient saved successfully");
            }
            window.editPatientId = null;
           await loadPatients();
            closeModal('addPatientModal');
            e.target.reset();
        }
        else {
            alert("Save Failed");
        }
    }
    catch (err) {
        console.error(err);
        alert("Error Saving Patient");
    }
});
/* ============ LOAD PATIENT FOR EDIT ============ */

async function loadPatientForEdit(patientId) {
    try {

        const response = await fetch(
            `${API_BASE_URL}/${patientId}`
        );
        const result = await response.json();
        console.log("API Response:", result);
        console.log("Patient:", result.Patient);
        console.log("Dependents:", result.Dependents);
        console.log(result);

        const patient = result.Patient;
        const dependents = result.Dependents || [];
        window.editPatientId = patient.PatientId;

        document.getElementById('MRN').value =
            patient.MRN || '';
        document.getElementById('firstname').value =
            patient.FirstName || '';
        document.getElementById('lastname').value =
            patient.LastName || '';
        document.getElementById('dob').value =
            patient.DOB
                ? patient.DOB.substring(0, 10)
                : '';
        document.getElementById('gender').value =
            patient.Gender || '';
        document.getElementById('SSN').value =
            patient.SSN || '';
        document.getElementById('bloodgroup').value =
            patient.BloodGroup || '';
        document.getElementById('phonenumber').value =
            patient.PhoneNumber || '';
        document.getElementById('email').value =
            patient.Email || '';
        document.getElementById('maritalstatus').value =
            patient.MaritalStatus || '';
        document.getElementById('nationality').value =
            patient.Nationality || '';
        document.getElementById('alternatenumber').value =
            patient.AlternateNumber || '';
        document.getElementById('emergencycontact').value =
            patient.EmergencyContact || '';
        document.getElementById('emergencyrelationship').value =
            patient.EmergencyRelationship || '';
        document.getElementById('streetaddress').value =
            patient.StreetAddress || '';
        document.getElementById('City').value =
            patient.City || '';
        document.getElementById('State').value =
            patient.State || '';
        document.getElementById('Zipcode').value =
            patient.ZipCode || '';
        document.getElementById('admissiontype').value =
            patient.AdmissionType || '';
        document.getElementById('ward').value =
            patient.Ward || '';
        document.getElementById('room').value =
            patient.Room || '';
        document.getElementById('referringphysician').value =
            patient.ReferringPhysician || '';
        document.getElementById('insuranceprovider').value =
            patient.InsuranceProvider || '';
        document.getElementById('policynumber').value =
            patient.PolicyNumber || '';
        document.getElementById('insurancetype').value =
            patient.InsuranceType || '';
        document.getElementById('dependentname1').value =
            dependents[0]?.DependentName || '';

        document.getElementById('dependentrelationship1').value =
            dependents[0]?.Relationship || '';

        document.getElementById('dependentdob1').value =
            dependents[0]?.DOB?.substring(0, 10) || '';

        document.getElementById('dependentname2').value =
            dependents[1]?.DependentName || '';

        document.getElementById('dependentrelationship2').value =
            dependents[1]?.Relationship || '';

        document.getElementById('dependentdob2').value =
            dependents[1]?.DOB?.substring(0, 10) || '';

        console.log("Opening modal...");
        const kicker =
            document.getElementById(
                'patientModalKicker'
            );

        const title =
            document.getElementById(
                'patientModalTitle'
            );

        if (kicker) {
            kicker.textContent =
                `MRN: ${patient.MRN}`;
        }

        if (title) {
            title.textContent =
                `${patient.FirstName} ${patient.LastName}`;
        }
        const personalTab =
            document.querySelector('[data-tab="personal"]');

        const personalPanel =
            document.getElementById('personal-panel');

        console.log('Personal Tab:', personalTab);
        console.log('Personal Panel:', personalPanel);

        document.querySelectorAll('.patient-tab')
            .forEach(t => t.classList.remove('active'));

        document.querySelectorAll('.patient-panel')
            .forEach(p => p.classList.remove('active'));

        personalTab?.classList.add('active');
        personalPanel?.classList.add('active');

        openModal('addPatientModal');
    }

       catch (error) {
            console.error(error);
        }
    
}
/* ============ ENCOUNTER SAVE ============ */
async function loadEncounter(patientId) {

    console.log("Loading encounter for Patient:", patientId);
    console.log("Edit clicked:", patientId);
    
    document.getElementById('chiefInput').value = '';
    document.getElementById('hpiInput').value = '';
    document.getElementById('pmhInput').value = '';
    document.getElementById('planInput').value = '';

    document.getElementById('v_bp').value = '';
    document.getElementById('v_hr').value = '';
    document.getElementById('v_temp').value = '';
    document.getElementById('v_rr').value = '';
    document.getElementById('v_spo2').value = '';
    document.getElementById('v_weight').value = '';
    try {
        const response = await fetch(
            `/api/encounter/patient/${patientId}`
        );
        console.log("Response Status:", response.status);

        const data = await response.json();
        console.log(
            "Returned PatientId:",
            data[0]?.PatientId
        );
        console.log("Encounter Returned:", data);
        if (!data || data.length === 0) {
            console.log("No encounters found.");
            return;
        }
        const encounter = data[0];
        if (!encounter) {
            console.log("No matching encounter found.");
            return;
        }
        document.getElementById('chiefInput').value =
            encounter.ChiefComplaint || '';
        document.getElementById('hpiInput').value =
            encounter.HPI || '';
        document.getElementById('pmhInput').value =
            encounter.PMH || '';
        document.getElementById('planInput').value =
            encounter.PlanOfCare || '';
        const vital = encounter.VitalSigns?.[0];
        if (vital) {
            document.getElementById('v_bp').value =
                vital.BloodPressure || '';
            document.getElementById('v_hr').value =
                vital.HeartRate || '';
            document.getElementById('v_temp').value =
                vital.Temperature || '';
            document.getElementById('v_rr').value =
                vital.RespiratoryRate || '';
            document.getElementById('v_spo2').value =
                vital.SPO2 || '';
            document.getElementById('v_weight').value =
                vital.Weight || '';
        }

    }
    catch (error) {

        console.error("Encounter Load Error:", error);
    }
}

document.getElementById('encounterSave')
    .addEventListener('click', async () => {
        const encounter = {
            PatientId: parseInt(activePatientId),
            ChiefComplaint:
                document.getElementById('chiefInput').value,
            HPI:
               document.getElementById('hpiInput').value,
            PMH:
                document.getElementById('pmhInput').value,
            PlanOfCare:
                document.getElementById('planInput').value,
            VitalSigns: [
                {
                    BloodPressure:
                        document.getElementById('v_bp').value,
                    HeartRate:
                        document.getElementById('v_hr').value,
                    Temperature:
                        document.getElementById('v_temp').value,
                    RespiratoryRate:
                        document.getElementById('v_rr').value,
                    SPO2:
                        document.getElementById('v_spo2').value,
                    Weight:
                        document.getElementById('v_weight').value
                }
            ]
        };
        try {
            const response = await fetch(
                "/api/encounter/save",
                {
                    method: "POST",
                    headers: {
                        "Content-Type":
                            "application/json"
                    },
                    body: JSON.stringify(encounter)
                });
            if (response.ok) {
                toast("Encounter saved successfully.");
            }
            else {
                toast("Failed to save encounter.");
            }

        }
        catch (error) {
            console.error(error);
            toast("Error saving encounter.");
        }
    });
document.getElementById('encounterCancel').addEventListener('click', () => {
    showTab('demographics');
});

/* ============ ATTACH DOCUMENT ============ */
document.getElementById('attachDocForm')
    .addEventListener('submit', async function (e) {
        e.preventDefault();

        try {

            const name =
                document.getElementById('doc_name').value;

            const type =
                document.getElementById('doc_type').value;

            const fileInput =
                document.getElementById('doc_file');

            if (!activePatientId) {
                toast("Please select a patient first.");
                return;
            }

            if (!fileInput.files.length) {
                toast("Please select a file.");
                return;
            }

            const formData = new FormData();

            formData.append(
                "PatientId",
                activePatientId);

            formData.append(
                "DocumentName",
                name);

            formData.append(
                "DocumentType",
                type);

            formData.append(
                "File",
                fileInput.files[0]);

            console.log("Uploading...");

            const response = await fetch(
                `${API_BASE_URL}/upload`,
                {
                    method: "POST",
                    body: formData
                });

            console.log("Status:", response.status);

            const result =
                await response.json();

            console.log(result);

            if (result.success) {

                await renderDocs();

                toast(
                    "Document uploaded successfully."
                );

                closeModal(
                    "attachDocModal"
                );

                this.reset();
            }
            else {

                toast(
                    result.message
                );
            }
        }
        catch (error) {

            console.error("UPLOAD ERROR:", error);

            alert(error);

            toast(
                "Upload failed."
            );
        
        }
    });
document.getElementById('attachDocBtn')
    .addEventListener('click', () => {
        openModal('attachDocModal');
    });
/* ============ SIDEBAR NAV / SIGN OUT ============ */
document.getElementById('navDashboard').addEventListener('click', () => {
    showTab('demographics');
    document.getElementById('sidebar').classList.remove('open');
});
document.getElementById('signOutBtn').addEventListener('click', () => {
    if (confirm('Are you sure you want to sign out?')) {
        window.location.href = '/Account/Logout';

    }
});
/* ============ MOBILE SIDEBAR TOGGLE ============ */
document.getElementById('sidebarToggle').addEventListener('click', () => {
    document.getElementById('sidebar').classList.toggle('open');
});
document
    .querySelectorAll('.patient-tab')
    .forEach(tab => {

        tab.addEventListener('click', () => {

            document
                .querySelectorAll('.patient-tab')
                .forEach(t =>
                    t.classList.remove('active'));

            document
                .querySelectorAll('.patient-panel')
                .forEach(panel =>
                    panel.classList.remove('active'));

            tab.classList.add('active');

            const panelId = `${tab.dataset.tab}-panel`;

            console.log("Opening panel:", panelId);

            const panel =
                document.getElementById(panelId);

            if (!panel) {
                console.error("Panel not found:", panelId);
                return;
            }

            panel.classList.add('active');
        });

    });
/* ============ INIT ============ */
loadPatients();
