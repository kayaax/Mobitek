var Footer = Vue.extend({
    data: {
        Footer: []
    },
    mounted() {
        this.loadFooter();
    },
    methods: {
        loadFooter() {
            axios({
                baseURL: '/Component/Form?Name=Footer',
                method: 'get',
                data: null,
                headers: {
                    'Accept': 'application/json',
                    'Content-Type': 'application/json',
                    'X-Response-View': 'Json'
                }
            }).then(response => {
                if (response.data !== "") {
                    this.Footer = response.data;
                }
            })
        }
    }
})
var footer = new Footer().$mount('#Footer')