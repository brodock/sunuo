all: sunuo.html

clean:
	rm -f sunuo.html

install: all
	install -m 0644 sunuo.html $(RUNUO_BASE)/

sunuo.html: sunuo.xml
	xsltproc -o $@ /usr/share/sgml/docbook/stylesheet/xsl/nwalsh/xhtml/docbook.xsl $<
